using System.Linq;
using SolutionErd.Model;
using Xunit;

namespace SolutionErd.Tests
{
    public class ShortcutTests
    {
        static DiagramEdge Edge(DiagramModel model, string child, string column) =>
            model.Edges.Single(e => e.ChildTable == child && e.ChildRow == column);

        [Fact]
        public void DenormalisedLookupIsMarked()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_project").Table("cp_structure").Table("cp_component")
                .Lookup("cp_structure", "cp_projectid", "cp_project")
                .Lookup("cp_component", "cp_structureid", "cp_structure")
                .Lookup("cp_component", "cp_projectid", "cp_project")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions());

            Assert.Equal("cp_structure", Edge(model, "cp_component", "cp_projectid").ShortcutVia);
            Assert.Null(Edge(model, "cp_component", "cp_structureid").ShortcutVia);
            Assert.Null(Edge(model, "cp_structure", "cp_projectid").ShortcutVia);
        }

        [Fact]
        public void PathsCanBeLongerThanOneHop()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_project").Table("cp_site").Table("cp_structure").Table("cp_component")
                .Lookup("cp_site", "cp_projectid", "cp_project")
                .Lookup("cp_structure", "cp_siteid", "cp_site")
                .Lookup("cp_component", "cp_structureid", "cp_structure")
                .Lookup("cp_component", "cp_projectid", "cp_project")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions());

            Assert.Equal("cp_structure", Edge(model, "cp_component", "cp_projectid").ShortcutVia);
        }

        [Fact]
        public void DifferentColumnNameIsNotAShortcut()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_project").Table("cp_task").Table("cp_person")
                .Lookup("cp_project", "cp_managerid", "cp_person")
                .Lookup("cp_task", "cp_projectid", "cp_project")
                .Lookup("cp_task", "cp_assignedtoid", "cp_person")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions());

            Assert.All(model.Edges, e => Assert.Null(e.ShortcutVia));
        }

        [Fact]
        public void RequiredLookupIsNotHiddenBehindOptionalPath()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_project").Table("cp_milestone").Table("cp_task")
                .Lookup("cp_milestone", "cp_projectid", "cp_project", required: true)
                .Lookup("cp_task", "cp_projectid", "cp_project", required: true)
                .Lookup("cp_task", "cp_milestoneid", "cp_milestone")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions());

            Assert.Null(Edge(model, "cp_task", "cp_projectid").ShortcutVia);
        }

        [Fact]
        public void TablesThatLookEachOtherUpAreNotAPath()
        {
            // The project's "last inspection" lookup is a reference, not a parent: it mustn't hide the
            // structure's own inspection-free lookups.
            var snapshot = new SnapshotBuilder()
                .Table("cp_project").Table("cp_inspection").Table("cp_site").Table("cp_structure")
                .Lookup("cp_project", "cp_lastinspectionid", "cp_inspection")
                .Lookup("cp_inspection", "cp_projectid", "cp_project")
                .Lookup("cp_inspection", "cp_siteid", "cp_site")
                .Lookup("cp_structure", "cp_projectid", "cp_project")
                .Lookup("cp_structure", "cp_siteid", "cp_site")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions());

            Assert.Null(Edge(model, "cp_structure", "cp_siteid").ShortcutVia);
            Assert.Null(Edge(model, "cp_structure", "cp_projectid").ShortcutVia);
        }

        [Theory]
        [InlineData("assets")]
        [InlineData("project-delivery")]
        public void EveryShortcutNamesALookupThatStaysDrawn(string fixture)
        {
            var model = NoiseFilter.Apply(Fixtures.Synthetic(fixture), new ErdOptions());

            var shortcuts = model.Edges.Where(e => e.ShortcutVia != null).ToList();
            Assert.NotEmpty(shortcuts);
            foreach (var e in shortcuts)
            {
                Assert.Contains(model.Edges, v => v.ChildTable == e.ChildTable && v.ParentTable == e.ShortcutVia && v.ShortcutVia == null);
            }
        }

        [Fact]
        public void ShortcutsAreRowTextUnlessAskedFor()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_project").Table("cp_structure").Table("cp_component")
                .Lookup("cp_structure", "cp_projectid", "cp_project")
                .Lookup("cp_component", "cp_structureid", "cp_structure")
                .Lookup("cp_component", "cp_projectid", "cp_project")
                .Build();

            var labelled = ErdBuilder.Build(snapshot, new ErdOptions()).Pages.Single();
            var drawn = ErdBuilder.Build(snapshot, new ErdOptions { DrawShortcutLookups = true }).Pages.Single();

            Assert.Equal(2, labelled.Edges.Count);
            Assert.Equal(3, drawn.Edges.Count);
            var row = labelled.Boxes.Single(b => b.Name == "cp_component").Row("cp_projectid");
            Assert.Equal("→ cp_project (via cp_structure)", row.Ref);
        }
    }
}
