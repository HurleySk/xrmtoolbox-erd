using System;
using System.Collections.Generic;
using System.Linq;

namespace SolutionErd.Layout
{
    /// <summary>How tangled a page is. Lower is better on every count.</summary>
    public sealed class LayoutScore
    {
        public string Page { get; set; }
        public int Boxes { get; set; }
        public int Edges { get; set; }
        public int Crossings { get; set; }
        public int Bends { get; set; }

        /// <summary>Line segments that pass through a box other than their own two ends.</summary>
        public int BoxHits { get; set; }

        public double Length { get; set; }

        /// <summary>
        /// Length where two different lines run on top of each other. Shared runs hide which line goes where,
        /// and <see cref="Crossings"/> doesn't see them. Not in <see cref="Score"/>, so older runs stay comparable.
        /// </summary>
        public double Overlap { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>Weighted total: box hits worst, then crossings, then bends, then ink.</summary>
        public double Score => 10 * BoxHits + 3 * Crossings + Bends + Length / 200;

        public override string ToString() =>
            $"{Page}: boxes {Boxes}, edges {Edges}, crossings {Crossings}, bends {Bends}, box hits {BoxHits}, " +
            $"overlap {Overlap:0}, length {Length:0}, size {Width:0}x{Height:0}, score {Score:0.0}";
    }

    public static class LayoutMetrics
    {
        public static LayoutScore Measure(PageLayout layout)
        {
            var score = new LayoutScore
            {
                Page = layout.Name,
                Boxes = layout.Boxes.Count,
                Edges = layout.Edges.Count,
                Width = layout.Width,
                Height = layout.Height,
            };

            var segments = layout.Edges.Select(e => Segments(e.Points).ToList()).ToList();
            score.Bends = layout.Edges.Sum(e => Math.Max(0, e.Points.Count - 2));
            score.Length = segments.SelectMany(s => s).Sum(s => s.Length);

            for (var i = 0; i < segments.Count; i++)
            {
                for (var j = i + 1; j < segments.Count; j++)
                {
                    foreach (var a in segments[i])
                    {
                        foreach (var b in segments[j])
                        {
                            if (Cross(a, b)) score.Crossings++;
                            score.Overlap += Shared(a, b);
                        }
                    }
                }
            }

            for (var i = 0; i < layout.Edges.Count; i++)
            {
                var e = layout.Edges[i];
                foreach (var box in layout.Boxes)
                {
                    if (box.Id == e.FromBox || box.Id == e.ToBox) continue;
                    if (segments[i].Any(s => HitsBox(s, box))) score.BoxHits++;
                }
            }

            return score;
        }

        struct Segment
        {
            public Pt A;
            public Pt B;
            public double Length => Math.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
        }

        static IEnumerable<Segment> Segments(IReadOnlyList<Pt> points)
        {
            for (var i = 0; i + 1 < points.Count; i++)
            {
                yield return new Segment { A = points[i], B = points[i + 1] };
            }
        }

        const double Eps = 0.5;

        /// <summary>Proper crossing: interiors intersect at a single point. Shared ends and overlaps don't count.</summary>
        static bool Cross(Segment s, Segment t)
        {
            var d1 = Orient(t.A, t.B, s.A);
            var d2 = Orient(t.A, t.B, s.B);
            var d3 = Orient(s.A, s.B, t.A);
            var d4 = Orient(s.A, s.B, t.B);
            if (!((d1 > Eps && d2 < -Eps) || (d1 < -Eps && d2 > Eps))) return false;
            if (!((d3 > Eps && d4 < -Eps) || (d3 < -Eps && d4 > Eps))) return false;
            return true;
        }

        /// <summary>Length of the part two axis-parallel segments share.</summary>
        static double Shared(Segment s, Segment t)
        {
            bool Horizontal(Segment x) => Math.Abs(x.A.Y - x.B.Y) < Eps;
            bool Vertical(Segment x) => Math.Abs(x.A.X - x.B.X) < Eps;
            if (Horizontal(s) && Horizontal(t) && Math.Abs(s.A.Y - t.A.Y) < Eps)
            {
                return Math.Max(0, Math.Min(Math.Max(s.A.X, s.B.X), Math.Max(t.A.X, t.B.X)) - Math.Max(Math.Min(s.A.X, s.B.X), Math.Min(t.A.X, t.B.X)));
            }

            if (Vertical(s) && Vertical(t) && Math.Abs(s.A.X - t.A.X) < Eps)
            {
                return Math.Max(0, Math.Min(Math.Max(s.A.Y, s.B.Y), Math.Max(t.A.Y, t.B.Y)) - Math.Max(Math.Min(s.A.Y, s.B.Y), Math.Min(t.A.Y, t.B.Y)));
            }

            return 0;
        }

        static double Orient(Pt a, Pt b, Pt c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        /// <summary>True when the segment passes through the inside of the box (touching its edge doesn't count).</summary>
        internal static bool SegmentHitsBox(Pt a, Pt b, BoxLayout box) => HitsBox(new Segment { A = a, B = b }, box);

        static bool HitsBox(Segment s, BoxLayout box)
        {
            var left = box.X + 1;
            var right = box.Right - 1;
            var top = box.Y + 1;
            var bottom = box.Bottom - 1;
            var minX = Math.Min(s.A.X, s.B.X);
            var maxX = Math.Max(s.A.X, s.B.X);
            var minY = Math.Min(s.A.Y, s.B.Y);
            var maxY = Math.Max(s.A.Y, s.B.Y);
            if (!(maxX > left && minX < right && maxY > top && minY < bottom)) return false;
            if (minX == maxX || minY == maxY) return true;

            // A diagonal segment: clip it to the box (Liang-Barsky) and see if anything is left.
            double t0 = 0, t1 = 1;
            var dx = s.B.X - s.A.X;
            var dy = s.B.Y - s.A.Y;
            bool Clip(double p, double q)
            {
                if (Math.Abs(p) < 1e-9) return q > 0;
                var r = q / p;
                if (p < 0) t0 = Math.Max(t0, r);
                else t1 = Math.Min(t1, r);
                return t0 < t1;
            }

            return Clip(-dx, s.A.X - left) && Clip(dx, right - s.A.X) && Clip(-dy, s.A.Y - top) && Clip(dy, bottom - s.A.Y);
        }
    }
}
