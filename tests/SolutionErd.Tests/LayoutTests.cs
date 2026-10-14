using System;
using System.Linq;
using SolutionErd.Layout;
using SolutionErd.Model;
using Xunit;

namespace SolutionErd.Tests
{
    public class LayoutTests
    {
        static ErdDocument Build(string fixture, ErdOptions options = null) =>
            ErdBuilder.Build(Fixtures.Synthetic(fixture), options ?? new ErdOptions());

        [Theory]
        [InlineData("assets")]
        [InlineData("project-delivery")]
        public void NoEdgePassesThroughABox(string fixture)
        {
            foreach (var page in Build(fixture).Pages)
            {
                Assert.Equal(0, LayoutMetrics.Measure(page).BoxHits);
            }
        }

        [Theory]
        [InlineData("assets")]
        [InlineData("project-delivery")]
        public void BoxesDoNotOverlap(string fixture)
        {
            foreach (var page in Build(fixture).Pages)
            {
                for (var i = 0; i < page.Boxes.Count; i++)
                {
                    for (var j = i + 1; j < page.Boxes.Count; j++)
                    {
                        var a = page.Boxes[i];
                        var b = page.Boxes[j];
                        var overlap = a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
                        Assert.False(overlap, $"{page.Name}: {a.Name} overlaps {b.Name}");
                    }
                }
            }
        }

        [Fact]
        public void EdgesLeaveFromLookupRow()
        {
            var page = Build("assets").Pages.Single();

            foreach (var edge in page.Edges)
            {
                var from = page.Box(edge.FromBox);
                var row = from.Row(edge.FromRow);
                Assert.NotNull(row);
                var start = edge.Points.First();
                Assert.Equal(from.Y + row.CenterOffset, start.Y, 1);
                Assert.Equal(edge.FromRight ? from.Right : from.X, start.X, 1);
            }
        }

        [Fact]
        public void EdgesEndAtTheParentKeyRow()
        {
            var page = Build("assets").Pages.Single();

            foreach (var edge in page.Edges.Where(e => !page.Box(e.ToBox).IsStub))
            {
                var to = page.Box(edge.ToBox);
                var end = edge.Points.Last();
                Assert.Equal(to.Y + to.Rows[0].CenterOffset, end.Y, 1);
            }
        }

        [Fact]
        public void SelfReferenceIsSideLoop()
        {
            var page = Build("assets").Pages.Single();

            var loop = page.Edges.Single(e => e.FromBox == e.ToBox);
            var box = page.Box(loop.FromBox);
            Assert.True(loop.FromRight && loop.ToRight);
            Assert.All(loop.Points, p => Assert.True(p.X >= box.Right - 0.5));
        }

        [Fact]
        public void EdgesAreOrthogonal()
        {
            foreach (var page in Build("project-delivery").Pages)
            {
                foreach (var edge in page.Edges)
                {
                    for (var i = 0; i + 1 < edge.Points.Count; i++)
                    {
                        var a = edge.Points[i];
                        var b = edge.Points[i + 1];
                        Assert.True(Math.Abs(a.X - b.X) < 0.5 || Math.Abs(a.Y - b.Y) < 0.5, $"{page.Name}/{edge.Id} has a diagonal segment");
                    }
                }
            }
        }

        [Fact]
        public void CrowsFootEndsMatchCardinality()
        {
            var page = Build("assets").Pages.Single();

            Assert.All(page.Edges, e => Assert.Equal(EndMark.ZeroOrMany, e.StartMark));
            var siteToAsset = page.Edges.Single(e => e.FromRow == "cp_siteid" && page.Box(e.FromBox).Name == "cp_asset");
            Assert.Equal(EndMark.One, siteToAsset.EndMark);
            var parentAsset = page.Edges.Single(e => e.FromRow == "cp_parentassetid");
            Assert.Equal(EndMark.ZeroOrOne, parentAsset.EndMark);
        }
    }
}
