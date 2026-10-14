using System.Collections.Generic;
using System.Linq;
using SolutionErd.Layout;
using Xunit;

namespace SolutionErd.Tests
{
    public class MetricsTests
    {
        static PageLayout Page(params List<Pt>[] lines)
        {
            var page = new PageLayout { Name = "test" };
            page.Boxes.Add(new BoxLayout { Id = "a", Name = "a", X = 0, Y = 0, Width = 10, Height = 10 });
            page.Boxes.Add(new BoxLayout { Id = "b", Name = "b", X = 200, Y = 200, Width = 10, Height = 10 });
            page.Boxes.Add(new BoxLayout { Id = "c", Name = "c", X = 100, Y = 0, Width = 40, Height = 40 });
            page.Edges.AddRange(lines.Select((points, i) => new EdgeLayout { Id = "e" + i, FromBox = "a", ToBox = "b", Points = points }));
            return page;
        }

        [Fact]
        public void DiagonalLinePastABoxCornerIsNotABoxHit()
        {
            // Its bounding rectangle covers box c, the line itself passes below it.
            var page = Page(new List<Pt> { new Pt(10, 10), new Pt(200, 200) });

            Assert.Equal(0, LayoutMetrics.Measure(page).BoxHits);
        }

        [Fact]
        public void DiagonalLineThroughABoxIsABoxHit()
        {
            var page = Page(new List<Pt> { new Pt(10, 10), new Pt(200, 30) });

            Assert.Equal(1, LayoutMetrics.Measure(page).BoxHits);
        }

        [Fact]
        public void OverlapCountsRunsTwoLinesShare()
        {
            var page = Page(
                new List<Pt> { new Pt(10, 5), new Pt(60, 5), new Pt(60, 200) },
                new List<Pt> { new Pt(10, 8), new Pt(60, 8), new Pt(60, 150), new Pt(200, 150) });

            var score = LayoutMetrics.Measure(page);
            Assert.Equal(142, score.Overlap, 1);
            Assert.Equal(0, score.Crossings);
        }
    }
}
