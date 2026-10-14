using System;
using System.Collections.Generic;
using System.Linq;
using SolutionErd.Model;

namespace SolutionErd
{
    /// <summary>Turns raw solution metadata into the tables, rows and lines worth drawing.</summary>
    public static class NoiseFilter
    {
        public static DiagramModel Apply(ErdSnapshot snapshot, ErdOptions options, NoiseRules rules = null)
        {
            rules = rules ?? NoiseRules.Default;
            var tables = snapshot.Tables
                .Where(t => !string.IsNullOrEmpty(t.LogicalName))
                .GroupBy(t => t.LogicalName)
                .ToDictionary(g => g.Key, g => g.First());

            bool IsIntersect(string name) => tables.TryGetValue(name, out var t) && t.IsIntersect;
            bool InSolution(string name) => tables.TryGetValue(name, out var t) && t.InSolution && !t.IsIntersect;
            string PrimaryKey(string name) =>
                tables.TryGetValue(name, out var t) && !string.IsNullOrEmpty(t.PrimaryIdAttribute) ? t.PrimaryIdAttribute : name + "id";

            var edges = new List<DiagramEdge>();
            var seen = new HashSet<string>();

            var oneToMany = snapshot.Relationships
                .Where(r => r.Kind == RelationshipKind.OneToMany)
                .OrderBy(r => r.ChildTable, StringComparer.Ordinal)
                .ThenBy(r => r.ChildColumn, StringComparer.Ordinal)
                .ThenBy(r => r.ParentTable, StringComparer.Ordinal);

            foreach (var rel in oneToMany)
            {
                var childIn = InSolution(rel.ChildTable);
                var parentIn = InSolution(rel.ParentTable);
                if (!childIn && !parentIn) continue;
                if (IsIntersect(rel.ChildTable) || IsIntersect(rel.ParentTable)) continue;

                var column = FindColumn(tables, rel.ChildTable, rel.ChildColumn);
                var isCustom = column?.IsCustom ?? rel.IsCustom;
                if (!options.ShowSystemRelationships && !isCustom)
                {
                    if (rules.IsSystemColumn(rel.ChildColumn)) continue;
                    if (rules.IsSystemTable(rel.ChildTable) || rules.IsSystemTable(rel.ParentTable)) continue;
                    if (!(childIn && parentIn)) continue;
                }

                if (!childIn && !options.ShowExternalChildren) continue;
                if ((column?.Targets?.Count ?? 1) > options.MaxPolymorphicTargets && !(childIn && parentIn)) continue;
                if (!(childIn && parentIn) && !options.ShowStubs) continue;
                if (!seen.Add($"{rel.ChildTable}|{rel.ChildColumn}|{rel.ParentTable}")) continue;

                edges.Add(new DiagramEdge
                {
                    Id = rel.SchemaName ?? $"{rel.ChildTable}_{rel.ChildColumn}_{rel.ParentTable}",
                    Kind = EdgeKind.OneToMany,
                    ParentTable = rel.ParentTable,
                    ParentRow = string.IsNullOrEmpty(rel.ParentColumn) ? PrimaryKey(rel.ParentTable) : rel.ParentColumn,
                    ChildTable = rel.ChildTable,
                    ChildRow = rel.ChildColumn,
                    ParentRequired = column?.IsRequired ?? false,
                });
            }

            var manyToMany = snapshot.Relationships
                .Where(r => r.Kind == RelationshipKind.ManyToMany)
                .OrderBy(r => r.SchemaName, StringComparer.Ordinal);

            foreach (var rel in manyToMany)
            {
                var aIn = InSolution(rel.ParentTable);
                var bIn = InSolution(rel.ChildTable);
                if (!aIn && !bIn) continue;
                if (!options.ShowSystemRelationships && !rel.IsCustom)
                {
                    if (!(aIn && bIn)) continue;
                    if (rules.IsSystemTable(rel.ParentTable) || rules.IsSystemTable(rel.ChildTable)) continue;
                }

                if (!(aIn && bIn) && !options.ShowStubs) continue;
                if (!seen.Add("nn|" + rel.SchemaName)) continue;

                edges.Add(new DiagramEdge
                {
                    Id = rel.SchemaName,
                    Kind = EdgeKind.ManyToMany,
                    ParentTable = rel.ParentTable,
                    ParentRow = PrimaryKey(rel.ParentTable),
                    ChildTable = rel.ChildTable,
                    ChildRow = PrimaryKey(rel.ChildTable),
                });
            }

            var model = new DiagramModel
            {
                Title = snapshot.SolutionDisplayName ?? snapshot.SolutionName,
                Edges = edges,
            };

            var names = tables.Values.Where(t => t.InSolution && !t.IsIntersect).Select(t => t.LogicalName)
                .Concat(edges.SelectMany(e => new[] { e.ParentTable, e.ChildTable }))
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal);

            foreach (var name in names)
            {
                tables.TryGetValue(name, out var info);
                var table = new DiagramTable
                {
                    Name = name,
                    DisplayName = string.IsNullOrWhiteSpace(info?.DisplayName) ? name : info.DisplayName,
                    InSolution = InSolution(name),
                };
                if (table.InSolution)
                {
                    table.Rows = BuildRows(info, edges.Where(e => e.ChildTable == name), options);
                }

                model.Tables.Add(table);
            }

            return model;
        }

        static ColumnInfo FindColumn(Dictionary<string, TableInfo> tables, string table, string column) =>
            table != null && column != null && tables.TryGetValue(table, out var t)
                ? t.Columns.FirstOrDefault(c => c.LogicalName == column)
                : null;

        static List<DiagramRow> BuildRows(TableInfo info, IEnumerable<DiagramEdge> childEdges, ErdOptions options)
        {
            var rows = new List<DiagramRow>();
            var columns = info.Columns.GroupBy(c => c.LogicalName).ToDictionary(g => g.Key, g => g.First());

            DiagramRow Row(string name, RowKind kind)
            {
                columns.TryGetValue(name, out var c);
                return new DiagramRow
                {
                    Name = name,
                    DisplayName = string.IsNullOrWhiteSpace(c?.DisplayName) ? name : c.DisplayName,
                    Kind = kind,
                    Type = c?.Type,
                    IsRequired = c?.IsRequired ?? false,
                };
            }

            var pk = info.PrimaryIdAttribute ?? info.Columns.FirstOrDefault(c => c.IsPrimaryId)?.LogicalName ?? info.LogicalName + "id";
            rows.Add(Row(pk, RowKind.PrimaryKey));

            var primaryName = info.PrimaryNameAttribute ?? info.Columns.FirstOrDefault(c => c.IsPrimaryName)?.LogicalName;
            if (!string.IsNullOrEmpty(primaryName) && primaryName != pk)
            {
                rows.Add(Row(primaryName, RowKind.PrimaryName));
            }

            var used = new HashSet<string>(rows.Select(r => r.Name));
            var lookups = childEdges.Where(e => e.Kind == EdgeKind.OneToMany).Select(e => e.ChildRow).Distinct();
            var extra = Enumerable.Empty<ColumnInfo>();
            if (options.Columns == ColumnMode.KeysLookupsAndCustom)
            {
                extra = info.Columns.Where(c => c.IsCustom);
            }
            else if (options.Columns == ColumnMode.All)
            {
                extra = info.Columns;
            }

            var rest = lookups.Select(l => Row(l, RowKind.Lookup))
                .Concat(extra.Select(c => Row(c.LogicalName, c.IsLookup ? RowKind.Lookup : RowKind.Column)))
                .Where(r => used.Add(r.Name))
                .OrderBy(r => r.Kind == RowKind.Lookup ? 0 : 1)
                .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase);

            rows.AddRange(rest);
            return rows;
        }
    }
}
