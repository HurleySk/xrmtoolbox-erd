using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionErd.Model;

namespace SolutionErd.Tests
{
    static class Fixtures
    {
        static readonly Lazy<string> Root = new Lazy<string>(() =>
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SolutionErd.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
        });

        public static string Path_(string relative) => Path.Combine(Root.Value, relative);

        public static ErdSnapshot Synthetic(string name) => ErdSnapshot.Load(Path_(Path.Combine("fixtures", "synthetic", name + ".json")));
    }

    /// <summary>Builds small snapshots in code for targeted tests.</summary>
    sealed class SnapshotBuilder
    {
        readonly ErdSnapshot snapshot = new ErdSnapshot { SolutionName = "test", SolutionDisplayName = "Test" };

        public SnapshotBuilder Table(string name, bool inSolution = true, bool custom = true, bool intersect = false)
        {
            snapshot.Tables.Add(new TableInfo
            {
                LogicalName = name,
                DisplayName = name,
                PrimaryIdAttribute = name + "id",
                PrimaryNameAttribute = intersect ? null : "name",
                InSolution = inSolution,
                IsCustom = custom,
                IsIntersect = intersect,
                Columns = intersect
                    ? new List<ColumnInfo>()
                    : new List<ColumnInfo>
                    {
                        new ColumnInfo { LogicalName = name + "id", Type = "Uniqueidentifier", IsPrimaryId = true },
                        new ColumnInfo { LogicalName = "name", Type = "String", IsPrimaryName = true },
                    },
            });
            return this;
        }

        public SnapshotBuilder Lookup(string child, string column, string parent, bool custom = true, bool required = false, params string[] moreTargets)
        {
            var table = snapshot.Tables.First(t => t.LogicalName == child);
            var targets = new[] { parent }.Concat(moreTargets).ToList();
            table.Columns.Add(new ColumnInfo
            {
                LogicalName = column,
                Type = targets.Count > 1 ? "Customer" : "Lookup",
                IsCustom = custom,
                IsRequired = required,
                Targets = targets,
            });
            foreach (var target in targets)
            {
                snapshot.Relationships.Add(new RelationshipInfo
                {
                    SchemaName = $"{target}_{child}_{column}",
                    Kind = RelationshipKind.OneToMany,
                    ParentTable = target,
                    ParentColumn = target + "id",
                    ChildTable = child,
                    ChildColumn = column,
                    IsCustom = custom,
                });
            }

            return this;
        }

        public SnapshotBuilder ManyToMany(string a, string b)
        {
            var intersect = a + "_" + b;
            Table(intersect, intersect: true);
            snapshot.Relationships.Add(new RelationshipInfo
            {
                SchemaName = intersect,
                Kind = RelationshipKind.ManyToMany,
                ParentTable = a,
                ChildTable = b,
                IntersectTable = intersect,
                IsCustom = true,
            });
            return this;
        }

        public ErdSnapshot Build() => snapshot;
    }
}
