using System.Collections.Generic;
using System.Linq;

namespace SolutionErd.Layout
{
    /// <summary>A point in page pixels, y down.</summary>
    public struct Pt
    {
        public Pt(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public override string ToString() => $"({X:0.#},{Y:0.#})";
    }

    /// <summary>Crow's-foot line ends.</summary>
    public enum EndMark { None, One, ZeroOrOne, ZeroOrMany }

    public sealed class RowLayout
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string SubText { get; set; }

        /// <summary>"→ Table" for a lookup whose target isn't drawn on the page.</summary>
        public string Ref { get; set; }
        public string Key { get; set; }
        public bool Bold { get; set; }

        /// <summary>Offset of the row's top from the box's top.</summary>
        public double Top { get; set; }

        public double Height { get; set; }
        public double CenterOffset => Top + Height / 2;
    }

    public sealed class BoxLayout
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public bool IsStub { get; set; }
        public bool IsArea { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double HeaderHeight { get; set; }
        public List<RowLayout> Rows { get; set; } = new List<RowLayout>();

        public double Right => X + Width;
        public double Bottom => Y + Height;
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;

        public RowLayout Row(string name) => name == null ? null : Rows.FirstOrDefault(r => r.Name == name);
    }

    public sealed class EdgeLayout
    {
        public string Id { get; set; }
        public string FromBox { get; set; }
        public string FromRow { get; set; }
        public bool FromRight { get; set; }
        public string ToBox { get; set; }
        public string ToRow { get; set; }
        public bool ToRight { get; set; }
        public List<Pt> Points { get; set; } = new List<Pt>();
        public EndMark StartMark { get; set; }
        public EndMark EndMark { get; set; }
        public string Label { get; set; }
    }

    public sealed class PageLayout
    {
        public string Name { get; set; }
        public bool IsOverview { get; set; }
        public List<BoxLayout> Boxes { get; set; } = new List<BoxLayout>();
        public List<EdgeLayout> Edges { get; set; } = new List<EdgeLayout>();
        public double Width { get; set; }
        public double Height { get; set; }

        public BoxLayout Box(string id) => Boxes.FirstOrDefault(b => b.Id == id);
    }

    public sealed class ErdDocument
    {
        public string Title { get; set; }
        public List<PageLayout> Pages { get; set; } = new List<PageLayout>();
    }
}
