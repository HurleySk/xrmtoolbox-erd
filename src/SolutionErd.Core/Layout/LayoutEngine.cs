using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.Rectilinear;
using SolutionErd.Model;

namespace SolutionErd.Layout
{
    /// <summary>Places boxes with a layered (Sugiyama) layout, then routes right-angle lines between rows.</summary>
    public sealed class LayoutEngine
    {
        readonly ErdOptions options;

        public LayoutEngine(ErdOptions options)
        {
            this.options = options;
        }

        public PageLayout Layout(DiagramPage page)
        {
            var boxes = page.Tables
                .Select((t, i) => BoxSizer.Size(t, page.IsOverview, options, "b" + i))
                .ToList();
            var byName = boxes.ToDictionary(b => b.Name);
            var edges = page.Edges.Where(e => byName.ContainsKey(e.ParentTable) && byName.ContainsKey(e.ChildTable)).ToList();

            Place(boxes, edges, byName);
            var routed = Route(edges, byName, page.IsOverview);

            var layout = new PageLayout { Name = page.Name, IsOverview = page.IsOverview, Boxes = boxes, Edges = routed };
            Normalize(layout);
            return layout;
        }

        void Place(List<BoxLayout> boxes, List<DiagramEdge> edges, Dictionary<string, BoxLayout> byName)
        {
            var graph = new GeometryGraph();
            var nodes = new Dictionary<string, Node>();
            foreach (var box in boxes)
            {
                var node = new Node(CurveFactory.CreateRectangle(box.Width, box.Height, new Point(0, 0)), box);
                graph.Nodes.Add(node);
                nodes[box.Name] = node;
            }

            var pairs = new HashSet<(string, string)>();
            foreach (var e in edges.Where(e => !e.IsSelf))
            {
                if (pairs.Contains((e.ChildTable, e.ParentTable)) || !pairs.Add((e.ParentTable, e.ChildTable))) continue;
                graph.Edges.Add(new Edge(nodes[e.ParentTable], nodes[e.ChildTable]));
            }

            var leftToRight = options.Direction == LayoutDirection.LeftToRight;
            var settings = new SugiyamaLayoutSettings
            {
                NodeSeparation = ErdStyle.NodeSeparation,
                LayerSeparation = ErdStyle.LayerSeparation,
            };
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.StraightLine;
            if (leftToRight) settings.Transformation = PlaneTransformation.Rotation(Math.PI / 2);

            LayoutHelpers.CalculateLayout(graph, settings, null, null);

            // MSAGL is y-up; flip to y-down, then make parents sit left of (or above) their children.
            var centers = nodes.ToDictionary(kv => kv.Key, kv => new Pt(kv.Value.Center.X, -kv.Value.Center.Y));
            double drift = 0;
            foreach (var e in graph.Edges)
            {
                var parent = centers[((BoxLayout)e.Source.UserData).Name];
                var child = centers[((BoxLayout)e.Target.UserData).Name];
                drift += leftToRight ? parent.X - child.X : parent.Y - child.Y;
            }

            var mirror = drift > 0;
            foreach (var box in boxes)
            {
                var c = centers[box.Name];
                var x = leftToRight && mirror ? -c.X : c.X;
                var y = !leftToRight && mirror ? -c.Y : c.Y;
                box.X = Math.Round(x - box.Width / 2);
                box.Y = Math.Round(y - box.Height / 2);
            }
        }

        List<EdgeLayout> Route(List<DiagramEdge> edges, Dictionary<string, BoxLayout> byName, bool overview)
        {
            var shapes = byName.Values.ToDictionary(
                b => b.Name,
                b => new Shape(CurveFactory.CreateRectangle(b.Width, b.Height, new Point(b.CenterX, b.CenterY))) { UserData = b });

            var router = new RectilinearEdgeRouter(shapes.Values, ErdStyle.RoutingPadding, 0, false);
            var pending = new List<(EdgeLayout layout, EdgeGeometry geometry)>();
            var result = new List<EdgeLayout>();
            var selfLoops = new Dictionary<string, int>();

            foreach (var e in edges)
            {
                var child = byName[e.ChildTable];
                var parent = byName[e.ParentTable];
                var layout = new EdgeLayout
                {
                    Id = e.Id,
                    FromBox = child.Id,
                    FromRow = child.Row(e.ChildRow)?.Name,
                    ToBox = parent.Id,
                    ToRow = parent.Row(e.ParentRow)?.Name,
                    Label = e.Label,
                    StartMark = overview ? EndMark.None : EndMark.ZeroOrMany,
                    EndMark = overview ? EndMark.None
                        : e.Kind == EdgeKind.ManyToMany ? EndMark.ZeroOrMany
                        : e.ParentRequired ? EndMark.One : EndMark.ZeroOrOne,
                };
                result.Add(layout);

                if (e.IsSelf)
                {
                    selfLoops.TryGetValue(child.Name, out var k);
                    selfLoops[child.Name] = k + 1;
                    var reach = child.Right + ErdStyle.SelfLoopReach + 8 * k;
                    var y1 = PortY(child, layout.FromRow);
                    var y2 = PortY(parent, layout.ToRow);
                    layout.FromRight = layout.ToRight = true;
                    layout.Points = new List<Pt> { new Pt(child.Right, y1), new Pt(reach, y1), new Pt(reach, y2), new Pt(child.Right, y2) };
                    continue;
                }

                ChooseSides(child, parent, out var fromRight, out var toRight);
                layout.FromRight = fromRight;
                layout.ToRight = toRight;
                var start = new Pt(fromRight ? child.Right : child.X, PortY(child, layout.FromRow));
                var end = new Pt(toRight ? parent.Right : parent.X, PortY(parent, layout.ToRow));

                var sourcePort = new FloatingPort(shapes[child.Name].BoundaryCurve, new Point(start.X, start.Y));
                var targetPort = new FloatingPort(shapes[parent.Name].BoundaryCurve, new Point(end.X, end.Y));
                shapes[child.Name].Ports.Insert(sourcePort);
                shapes[parent.Name].Ports.Insert(targetPort);
                var geometry = new EdgeGeometry { SourcePort = sourcePort, TargetPort = targetPort };
                router.AddEdgeGeometryToRoute(geometry);
                pending.Add((layout, geometry));
                layout.Points = Fallback(start, end, fromRight, toRight);
            }

            if (pending.Count > 0)
            {
                try
                {
                    router.Run();
                    foreach (var (layout, geometry) in pending)
                    {
                        var points = Points(geometry.Curve);
                        if (points.Count >= 2) layout.Points = points;
                    }
                }
                catch (Exception)
                {
                    // keep the fallback routes
                }
            }

            return result;
        }

        static void ChooseSides(BoxLayout child, BoxLayout parent, out bool fromRight, out bool toRight)
        {
            var overlap = child.X < parent.Right && parent.X < child.Right;
            if (overlap)
            {
                fromRight = toRight = child.CenterX >= parent.CenterX;
                return;
            }

            fromRight = parent.CenterX > child.CenterX;
            toRight = !fromRight;
        }

        static double PortY(BoxLayout box, string row)
        {
            var r = box.Row(row);
            return r != null ? box.Y + r.CenterOffset : box.Y + Math.Min(box.HeaderHeight, box.Height) / 2;
        }

        static List<Pt> Fallback(Pt start, Pt end, bool fromRight, bool toRight)
        {
            var reach = ErdStyle.RoutingPadding * 2;
            var x1 = start.X + (fromRight ? reach : -reach);
            var x2 = end.X + (toRight ? reach : -reach);
            var mid = fromRight == toRight ? (fromRight ? Math.Max(x1, x2) : Math.Min(x1, x2)) : (x1 + x2) / 2;
            return Simplify(new List<Pt> { start, new Pt(mid, start.Y), new Pt(mid, end.Y), end });
        }

        static List<Pt> Points(ICurve curve)
        {
            var points = new List<Pt>();
            switch (curve)
            {
                case Polyline polyline:
                    points.AddRange(polyline.PolylinePoints.Select(p => new Pt(p.Point.X, p.Point.Y)));
                    break;
                case Curve c:
                    points.Add(new Pt(c.Start.X, c.Start.Y));
                    points.AddRange(c.Segments.Select(s => new Pt(s.End.X, s.End.Y)));
                    break;
                case null:
                    break;
                default:
                    points.Add(new Pt(curve.Start.X, curve.Start.Y));
                    points.Add(new Pt(curve.End.X, curve.End.Y));
                    break;
            }

            return Simplify(points);
        }

        /// <summary>Drops repeated and collinear points.</summary>
        internal static List<Pt> Simplify(List<Pt> points)
        {
            var result = new List<Pt>();
            foreach (var p in points)
            {
                if (result.Count > 0 && Near(result[result.Count - 1], p)) continue;
                if (result.Count >= 2)
                {
                    var a = result[result.Count - 2];
                    var b = result[result.Count - 1];
                    if ((Math.Abs(a.X - b.X) < 0.01 && Math.Abs(b.X - p.X) < 0.01) || (Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(b.Y - p.Y) < 0.01))
                    {
                        result[result.Count - 1] = p;
                        continue;
                    }
                }

                result.Add(p);
            }

            return result;
        }

        static bool Near(Pt a, Pt b) => Math.Abs(a.X - b.X) < 0.01 && Math.Abs(a.Y - b.Y) < 0.01;

        static void Normalize(PageLayout layout)
        {
            if (layout.Boxes.Count == 0) return;
            var xs = layout.Boxes.SelectMany(b => new[] { b.X, b.Right }).Concat(layout.Edges.SelectMany(e => e.Points.Select(p => p.X)));
            var ys = layout.Boxes.SelectMany(b => new[] { b.Y, b.Bottom }).Concat(layout.Edges.SelectMany(e => e.Points.Select(p => p.Y)));
            var minX = xs.Min();
            var minY = ys.Min();
            var dx = Math.Round(ErdStyle.PageMargin - minX);
            var dy = Math.Round(ErdStyle.PageMargin - minY);

            foreach (var b in layout.Boxes)
            {
                b.X = Math.Round(b.X + dx, 1);
                b.Y = Math.Round(b.Y + dy, 1);
            }

            foreach (var e in layout.Edges)
            {
                e.Points = e.Points.Select(p => new Pt(Math.Round(p.X + dx, 1), Math.Round(p.Y + dy, 1))).ToList();
            }

            layout.Width = Math.Ceiling(layout.Boxes.Max(b => b.Right)
                .Max(layout.Edges.SelectMany(e => e.Points).Select(p => p.X).DefaultIfEmpty(0).Max()) + ErdStyle.PageMargin);
            layout.Height = Math.Ceiling(layout.Boxes.Max(b => b.Bottom)
                .Max(layout.Edges.SelectMany(e => e.Points).Select(p => p.Y).DefaultIfEmpty(0).Max()) + ErdStyle.PageMargin);
        }
    }

    static class DoubleExtensions
    {
        public static double Max(this double a, double b) => Math.Max(a, b);
    }
}
