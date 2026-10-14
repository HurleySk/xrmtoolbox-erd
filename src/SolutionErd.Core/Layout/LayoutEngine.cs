using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
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

            List<EdgeLayout> routed;
            if (page.IsOverview && options.OverviewStyle != OverviewStyle.Layered)
            {
                PlaceSpread(boxes, edges);
                routed = options.OverviewStyle == OverviewStyle.SpreadStraight
                    ? RouteStraight(edges, byName)
                    : RouteFromCentres(edges, byName);
            }
            else
            {
                Place(boxes, edges, byName);
                routed = Route(edges, byName, page.IsOverview);
            }

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

        /// <summary>Places boxes in two dimensions by graph distance (MDS), then pushes overlapping boxes apart.</summary>
        static void PlaceSpread(List<BoxLayout> boxes, List<DiagramEdge> edges)
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

            var settings = new MdsLayoutSettings
            {
                RemoveOverlaps = true,
                NodeSeparation = ErdStyle.NodeSeparation * 3,
            };
            settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.StraightLine;
            LayoutHelpers.CalculateLayout(graph, settings, null, null);

            foreach (var box in boxes)
            {
                var c = nodes[box.Name].Center;
                box.X = Math.Round(c.X - box.Width / 2);
                box.Y = Math.Round(-c.Y - box.Height / 2);
            }
        }

        /// <summary>
        /// Straight lines between box centres, cut off at the box edges. A line that would cross another box
        /// curves around it instead.
        /// </summary>
        static List<EdgeLayout> RouteStraight(List<DiagramEdge> edges, Dictionary<string, BoxLayout> byName)
        {
            var result = new List<EdgeLayout>();
            var blocked = new List<(EdgeLayout layout, BoxLayout child, BoxLayout parent)>();
            foreach (var e in edges.Where(e => !e.IsSelf))
            {
                var child = byName[e.ChildTable];
                var parent = byName[e.ParentTable];
                var a = new Pt(child.CenterX, child.CenterY);
                var b = new Pt(parent.CenterX, parent.CenterY);
                var layout = new EdgeLayout
                {
                    Id = e.Id,
                    FromBox = child.Id,
                    ToBox = parent.Id,
                    Label = e.Label,
                    Straight = true,
                    FromRight = b.X >= a.X,
                    ToRight = a.X > b.X,
                    Points = new List<Pt> { Exit(a, b, child), Exit(b, a, parent) },
                };
                result.Add(layout);
                if (byName.Values.Any(box => box != child && box != parent && LayoutMetrics.SegmentHitsBox(layout.Points[0], layout.Points[1], box)))
                {
                    blocked.Add((layout, child, parent));
                }
            }

            if (blocked.Count > 0) CurveAround(blocked, byName);
            return result;
        }

        /// <summary>Routes the given lines as smooth curves that keep clear of every box, sampled into short segments.</summary>
        static void CurveAround(List<(EdgeLayout layout, BoxLayout child, BoxLayout parent)> blocked, Dictionary<string, BoxLayout> byName)
        {
            var graph = new GeometryGraph();
            var nodes = new Dictionary<BoxLayout, Node>();
            foreach (var box in byName.Values)
            {
                var node = new Node(CurveFactory.CreateRectangle(box.Width, box.Height, new Point(box.CenterX, box.CenterY)), box);
                graph.Nodes.Add(node);
                nodes[box] = node;
            }

            var routed = new List<(EdgeLayout layout, Edge edge, BoxLayout child, BoxLayout parent)>();
            foreach (var (layout, child, parent) in blocked)
            {
                var edge = new Edge(nodes[child], nodes[parent]);
                graph.Edges.Add(edge);
                routed.Add((layout, edge, child, parent));
            }

            try
            {
                new SplineRouter(graph, ErdStyle.RoutingPadding, ErdStyle.RoutingPadding * 2, Math.PI / 6, null).Run();
            }
            catch (Exception)
            {
                return; // keep the straight lines
            }

            foreach (var (layout, edge, child, parent) in routed)
            {
                var curve = edge.Curve;
                if (curve == null) continue;
                const int samples = 24;
                var points = Enumerable.Range(0, samples + 1)
                    .Select(i => curve[curve.ParStart + (curve.ParEnd - curve.ParStart) * i / samples])
                    .Select(p => new Pt(p.X, p.Y))
                    .ToList();
                points = TrimInside(points, child, fromStart: true);
                points = TrimInside(points, parent, fromStart: false);
                if (points.Count >= 2) layout.Points = points;
            }
        }

        /// <summary>Right-angle lines that may leave a box from any side: routed from the centres, cut at the edges.</summary>
        static List<EdgeLayout> RouteFromCentres(List<DiagramEdge> edges, Dictionary<string, BoxLayout> byName)
        {
            var shapes = byName.Values.ToDictionary(
                b => b.Name,
                b => new Shape(CurveFactory.CreateRectangle(b.Width, b.Height, new Point(b.CenterX, b.CenterY))) { UserData = b });
            var router = new RectilinearEdgeRouter(shapes.Values, ErdStyle.RoutingPadding, 0, false);
            var pending = new List<(EdgeLayout layout, EdgeGeometry geometry, BoxLayout child, BoxLayout parent)>();
            foreach (var e in edges.Where(e => !e.IsSelf))
            {
                var child = byName[e.ChildTable];
                var parent = byName[e.ParentTable];
                var layout = new EdgeLayout { Id = e.Id, FromBox = child.Id, ToBox = parent.Id, Label = e.Label };
                var sourcePort = new FloatingPort(shapes[child.Name].BoundaryCurve, new Point(child.CenterX, child.CenterY));
                var targetPort = new FloatingPort(shapes[parent.Name].BoundaryCurve, new Point(parent.CenterX, parent.CenterY));
                shapes[child.Name].Ports.Insert(sourcePort);
                shapes[parent.Name].Ports.Insert(targetPort);
                var geometry = new EdgeGeometry { SourcePort = sourcePort, TargetPort = targetPort };
                router.AddEdgeGeometryToRoute(geometry);
                pending.Add((layout, geometry, child, parent));
            }

            router.Run();
            foreach (var (layout, geometry, child, parent) in pending)
            {
                var points = Points(geometry.Curve);
                if (points.Count < 2)
                {
                    points = new List<Pt> { new Pt(child.CenterX, child.CenterY), new Pt(parent.CenterX, parent.CenterY) };
                    layout.Straight = true;
                }

                points = TrimInside(points, child, fromStart: true);
                points = TrimInside(points, parent, fromStart: false);
                layout.Points = Simplify(points);
                layout.FromRight = layout.Points[0].X >= child.Right - 0.5;
                layout.ToRight = layout.Points[layout.Points.Count - 1].X >= parent.Right - 0.5;
            }

            return pending.Select(p => p.layout).ToList();
        }

        /// <summary>Drops the part of a route that runs inside its end box, so it starts or ends on the box edge.</summary>
        static List<Pt> TrimInside(List<Pt> points, BoxLayout box, bool fromStart)
        {
            var list = fromStart ? points.ToList() : Enumerable.Reverse(points).ToList();
            bool Inside(Pt p) => p.X > box.X + 0.5 && p.X < box.Right - 0.5 && p.Y > box.Y + 0.5 && p.Y < box.Bottom - 0.5;
            while (list.Count > 2 && Inside(list[1])) list.RemoveAt(0);
            if (Inside(list[0])) list[0] = Exit(list[0], list[1], box);
            if (!fromStart) list.Reverse();
            return list;
        }

        /// <summary>Where the segment from <paramref name="inside"/> towards <paramref name="toward"/> leaves the box.</summary>
        internal static Pt Exit(Pt inside, Pt toward, BoxLayout box)
        {
            var dx = toward.X - inside.X;
            var dy = toward.Y - inside.Y;
            var t = 1.0;
            if (dx > 0) t = Math.Min(t, (box.Right - inside.X) / dx);
            else if (dx < 0) t = Math.Min(t, (box.X - inside.X) / dx);
            if (dy > 0) t = Math.Min(t, (box.Bottom - inside.Y) / dy);
            else if (dy < 0) t = Math.Min(t, (box.Y - inside.Y) / dy);
            t = Math.Max(0, t);
            return new Pt(inside.X + dx * t, inside.Y + dy * t);
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
