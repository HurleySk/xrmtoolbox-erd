using System.Linq;
using SolutionErd.Model;
using Xunit;

namespace SolutionErd.Tests
{
    public class ClustererTests
    {
        static readonly ErdOptions Options = new ErdOptions();

        static (DiagramModel model, System.Collections.Generic.List<DiagramPage> pages) Paginate(string fixture, ErdOptions options = null)
        {
            options = options ?? Options;
            var model = NoiseFilter.Apply(Fixtures.Synthetic(fixture), options);
            return (model, Clusterer.Paginate(model, options));
        }

        [Fact]
        public void SmallSolutionIsOnePage()
        {
            var (_, pages) = Paginate("assets");

            var page = Assert.Single(pages);
            Assert.False(page.IsOverview);
        }

        [Fact]
        public void LargeSolutionGetsOverviewFirst()
        {
            var (_, pages) = Paginate("project-delivery");

            Assert.True(pages.Count > 2);
            Assert.True(pages[0].IsOverview);
            Assert.All(pages.Skip(1), p => Assert.False(p.IsOverview));
        }

        [Fact]
        public void NoPageExceedsMaxTables()
        {
            var (_, pages) = Paginate("project-delivery");

            Assert.All(pages.Where(p => !p.IsOverview), p => Assert.True(p.Tables.Count(t => !t.AsStub) <= Options.MaxTablesPerPage, p.Name));
        }

        [Fact]
        public void EveryTableHasOneHomePage()
        {
            var (model, pages) = Paginate("project-delivery");

            foreach (var table in model.Tables.Where(t => t.InSolution))
            {
                var homes = pages.Where(p => !p.IsOverview && p.Tables.Any(t => !t.AsStub && t.Table.Name == table.Name)).ToList();
                Assert.True(homes.Count == 1, $"{table.Name} has {homes.Count} home pages");
            }
        }

        [Fact]
        public void HubsAreRowLabelsOnOtherPages()
        {
            var (_, pages) = Paginate("project-delivery");

            var detail = pages.Where(p => !p.IsOverview).ToList();
            Assert.Single(detail, p => p.Tables.Any(t => !t.AsStub && t.Table.Name == "cp_project"));
            Assert.DoesNotContain(detail, p => p.Tables.Any(t => t.AsStub && t.Table.Name == "cp_project"));
            Assert.Contains(detail, p => p.Tables.Any(t => t.RowRefs.TryGetValue("cp_projectid", out var refs) && refs.Contains("Project")));
        }

        [Fact]
        public void HubsBecomeStubsOnOtherPagesInStubMode()
        {
            var (_, pages) = Paginate("project-delivery", new ErdOptions { OffPageLinks = OffPageLinks.Stubs });

            var detail = pages.Where(p => !p.IsOverview).ToList();
            Assert.Single(detail, p => p.Tables.Any(t => !t.AsStub && t.Table.Name == "cp_project"));
            Assert.Contains(detail, p => p.Tables.Any(t => t.AsStub && t.Table.Name == "cp_project" && t.Note == "see " + HomeOf(detail, "cp_project")));
        }

        [Fact]
        public void LookupsFromOtherPagesAreNotDrawnByDefault()
        {
            var (_, pages) = Paginate("project-delivery");

            foreach (var page in pages.Where(p => !p.IsOverview))
            {
                var full = page.Tables.Where(t => !t.AsStub).Select(t => t.Table.Name).ToList();
                Assert.All(page.Edges.Where(e => e.Kind == EdgeKind.OneToMany), e => Assert.Contains(e.ChildTable, full));
            }
        }

        [Fact]
        public void PaginationIsDeterministic()
        {
            var a = Paginate("project-delivery").pages.Select(p => p.Name + ":" + string.Join(",", p.Tables.Select(t => t.Table.Name)));
            var b = Paginate("project-delivery").pages.Select(p => p.Name + ":" + string.Join(",", p.Tables.Select(t => t.Table.Name)));

            Assert.Equal(a, b);
        }

        static string HomeOf(System.Collections.Generic.List<DiagramPage> pages, string table) =>
            pages.Single(p => p.Tables.Any(t => !t.AsStub && t.Table.Name == table)).Name;
    }
}
