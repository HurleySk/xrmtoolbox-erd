using System.Linq;
using SolutionErd.Model;
using Xunit;

namespace SolutionErd.Tests
{
    public class NoiseFilterTests
    {
        static ErdSnapshot Sample() => new SnapshotBuilder()
            .Table("cp_project")
            .Table("cp_task")
            .Table("systemuser", inSolution: false, custom: false)
            .Table("account", inSolution: false, custom: false)
            .Lookup("cp_task", "cp_projectid", "cp_project", required: true)
            .Lookup("cp_task", "createdby", "systemuser", custom: false)
            .Lookup("cp_task", "cp_assignedtoid", "systemuser")
            .Lookup("cp_project", "cp_customerid", "account")
            .Build();

        [Fact]
        public void HidesSystemRelationshipsByDefault()
        {
            var model = NoiseFilter.Apply(Sample(), new ErdOptions());

            Assert.DoesNotContain(model.Edges, e => e.ChildRow == "createdby");
            Assert.Contains(model.Edges, e => e.ChildRow == "cp_assignedtoid");
        }

        [Fact]
        public void ShowsSystemRelationshipsWhenAsked()
        {
            var model = NoiseFilter.Apply(Sample(), new ErdOptions { ShowSystemRelationships = true });

            Assert.Contains(model.Edges, e => e.ChildRow == "createdby");
        }

        [Fact]
        public void OutOfSolutionTargetsBecomeStubs()
        {
            var model = NoiseFilter.Apply(Sample(), new ErdOptions());

            var account = model.Table("account");
            Assert.NotNull(account);
            Assert.False(account.InSolution);
            Assert.Empty(account.Rows);
        }

        [Fact]
        public void NoStubsOptionDropsLinesToOutsideTables()
        {
            var model = NoiseFilter.Apply(Sample(), new ErdOptions { ShowStubs = false });

            Assert.Null(model.Table("account"));
            Assert.All(model.Edges, e => Assert.True(model.Table(e.ParentTable).InSolution));
        }

        [Fact]
        public void LookupRowsFollowTheKeyRows()
        {
            var model = NoiseFilter.Apply(Sample(), new ErdOptions());

            var rows = model.Table("cp_task").Rows;
            Assert.Equal(RowKind.PrimaryKey, rows[0].Kind);
            Assert.Equal(RowKind.PrimaryName, rows[1].Kind);
            Assert.Equal(new[] { "cp_assignedtoid", "cp_projectid" }, rows.Skip(2).Select(r => r.Name).OrderBy(n => n));
            Assert.True(model.Edges.Single(e => e.ChildRow == "cp_projectid").ParentRequired);
        }

        [Fact]
        public void ManyToManyCollapsesIntersect()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_resource")
                .Table("cp_skill")
                .ManyToMany("cp_resource", "cp_skill")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions());

            var edge = Assert.Single(model.Edges);
            Assert.Equal(EdgeKind.ManyToMany, edge.Kind);
            Assert.Null(model.Table("cp_resource_cp_skill"));
        }

        [Fact]
        public void WidePolymorphicLookupsOnlyDrawToSolutionTables()
        {
            var snapshot = new SnapshotBuilder()
                .Table("cp_note")
                .Table("cp_project")
                .Table("account", inSolution: false, custom: false)
                .Table("contact", inSolution: false, custom: false)
                .Table("lead", inSolution: false, custom: false)
                .Lookup("cp_note", "cp_regardingid", "cp_project", true, false, "account", "contact", "lead")
                .Build();

            var model = NoiseFilter.Apply(snapshot, new ErdOptions { MaxPolymorphicTargets = 3 });

            var edge = Assert.Single(model.Edges);
            Assert.Equal("cp_project", edge.ParentTable);
        }

        [Fact]
        public void SyntheticSolutionDropsPlatformTables()
        {
            var model = NoiseFilter.Apply(Fixtures.Synthetic("project-delivery"), new ErdOptions());

            Assert.DoesNotContain(model.Tables, t => t.Name == "annotation" || t.Name == "activitypointer" || t.Name == "team");
            Assert.DoesNotContain(model.Edges, e => e.ChildRow == "ownerid" || e.ChildRow == "transactioncurrencyid");
        }
    }
}
