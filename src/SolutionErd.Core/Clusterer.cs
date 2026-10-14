using System;
using System.Collections.Generic;
using System.Linq;
using SolutionErd.Model;

namespace SolutionErd
{
    /// <summary>Splits a diagram into subject-area pages with hub tables shown as stubs away from home.</summary>
    public static class Clusterer
    {
        const int MinGroupSize = 4;

        public static List<DiagramPage> Paginate(DiagramModel model, ErdOptions options, NoiseRules rules = null)
        {
            rules = rules ?? NoiseRules.Default;
            var solution = model.Tables.Where(t => t.InSolution).ToList();
            if (!options.SplitIntoPages || solution.Count <= options.MaxTablesPerPage)
            {
                var all = new HashSet<string>(solution.Select(t => t.Name));
                return new List<DiagramPage> { BuildPage(model, model.Title ?? "All tables", all, new Dictionary<string, string>(), options) };
            }

            var index = solution.Select((t, i) => (t.Name, i)).ToDictionary(x => x.Name, x => x.i);
            var weights = new Dictionary<(int, int), double>();
            foreach (var e in model.Edges.Where(e => !e.IsSelf))
            {
                if (!index.TryGetValue(e.ParentTable, out var a) || !index.TryGetValue(e.ChildTable, out var b)) continue;
                var key = (Math.Min(a, b), Math.Max(a, b));
                weights.TryGetValue(key, out var w);
                weights[key] = w + 1;
            }

            var n = solution.Count;
            var neighbours = Enumerable.Range(0, n).Select(_ => new HashSet<int>()).ToList();
            foreach (var (a, b) in weights.Keys)
            {
                neighbours[a].Add(b);
                neighbours[b].Add(a);
            }

            double Weight(int a, int b) => weights.TryGetValue((Math.Min(a, b), Math.Max(a, b)), out var w) ? w : 0;

            var maxHubs = Math.Max(1, (int)Math.Ceiling(n / 10.0));
            var hubs = new HashSet<int>(Enumerable.Range(0, n)
                .Where(i => rules.IsCoreHub(solution[i].Name) || neighbours[i].Count >= options.HubDegreeThreshold)
                .OrderByDescending(i => rules.IsCoreHub(solution[i].Name))
                .ThenByDescending(i => neighbours[i].Count)
                .ThenBy(i => i)
                .Take(maxHubs + solution.Count(t => rules.IsCoreHub(t.Name))));

            var others = Enumerable.Range(0, n).Where(i => !hubs.Contains(i)).ToList();
            var groups = Detect(others, Weight);

            // Singletons join their strongest non-hub group, else gather around their strongest hub.
            var singles = groups.Where(g => g.Count == 1).Select(g => g[0]).ToList();
            groups = groups.Where(g => g.Count > 1).ToList();
            var byHub = new Dictionary<int, List<int>>();
            var standalone = new List<int>();
            foreach (var s in singles)
            {
                var best = groups
                    .Select(g => (g, w: g.Sum(m => Weight(s, m))))
                    .Where(x => x.w > 0)
                    .OrderByDescending(x => x.w)
                    .Select(x => x.g)
                    .FirstOrDefault();
                if (best != null && best.Count < options.MaxTablesPerPage)
                {
                    best.Add(s);
                    continue;
                }

                var hub = hubs.Where(h => Weight(s, h) > 0).OrderByDescending(h => Weight(s, h)).ThenBy(h => h).Cast<int?>().FirstOrDefault();
                if (hub.HasValue)
                {
                    if (!byHub.TryGetValue(hub.Value, out var list)) byHub[hub.Value] = list = new List<int>();
                    list.Add(s);
                }
                else
                {
                    standalone.Add(s);
                }
            }

            groups.AddRange(byHub.OrderBy(kv => kv.Key).Select(kv => kv.Value));
            groups = SplitLarge(groups, Weight, options.MaxTablesPerPage);
            groups = PackSmall(groups, options.MaxTablesPerPage);
            if (standalone.Count > 0) groups.AddRange(Chunk(standalone, options.MaxTablesPerPage));

            // Hubs live on the page they connect to most; the rest of the pages show them as stubs.
            var homeless = new List<int>();
            foreach (var h in hubs.OrderBy(h => h))
            {
                var home = groups
                    .Select(g => (g, w: g.Sum(m => Weight(h, m))))
                    .Where(x => x.w > 0)
                    .OrderByDescending(x => x.w)
                    .Select(x => x.g)
                    .FirstOrDefault();
                if (home != null) home.Add(h);
                else homeless.Add(h);
            }

            if (homeless.Count > 0) groups.AddRange(Chunk(homeless, options.MaxTablesPerPage));

            groups = groups.Select(g => g.OrderBy(i => i).ToList()).OrderByDescending(g => g.Count).ThenBy(g => g[0]).ToList();

            var pageNames = NamePages(groups, solution, Weight);
            var homePage = new Dictionary<string, string>();
            for (var p = 0; p < groups.Count; p++)
            {
                foreach (var i in groups[p]) homePage[solution[i].Name] = pageNames[p];
            }

            var pages = new List<DiagramPage> { BuildOverview(model, groups, solution, pageNames, homePage) };
            for (var p = 0; p < groups.Count; p++)
            {
                var full = new HashSet<string>(groups[p].Select(i => solution[i].Name));
                pages.Add(BuildPage(model, pageNames[p], full, homePage, options));
            }

            return pages;
        }

        static List<List<int>> Detect(List<int> nodes, Func<int, int, double> weight, double resolution = 1.0)
        {
            var edges = new List<(int, int, double)>();
            for (var x = 0; x < nodes.Count; x++)
            {
                for (var y = x + 1; y < nodes.Count; y++)
                {
                    var w = weight(nodes[x], nodes[y]);
                    if (w > 0) edges.Add((x, y, w));
                }
            }

            return Louvain.Detect(nodes.Count, edges, resolution)
                .Select(c => c.Select(l => nodes[l]).ToList())
                .ToList();
        }

        static List<List<int>> SplitLarge(List<List<int>> groups, Func<int, int, double> weight, int max)
        {
            var result = new List<List<int>>();
            var queue = new Queue<(List<int> g, double res)>(groups.Select(g => (g, 1.0)));
            while (queue.Count > 0)
            {
                var (g, res) = queue.Dequeue();
                if (g.Count <= max)
                {
                    result.Add(g);
                    continue;
                }

                var parts = res < 16 ? Detect(g, weight, res * 2) : null;
                if (parts == null || parts.Count < 2)
                {
                    if (res < 16)
                    {
                        queue.Enqueue((g, res * 2));
                        continue;
                    }

                    result.AddRange(Chunk(g, max));
                    continue;
                }

                foreach (var part in parts) queue.Enqueue((part, res * 2));
            }

            return result;
        }

        static List<List<int>> PackSmall(List<List<int>> groups, int max)
        {
            var big = groups.Where(g => g.Count >= MinGroupSize).ToList();
            var bins = new List<List<int>>();
            foreach (var g in groups.Where(g => g.Count < MinGroupSize).OrderByDescending(g => g.Count).ThenBy(g => g.Min()))
            {
                var bin = bins.FirstOrDefault(b => b.Count + g.Count <= max);
                if (bin == null) bins.Add(bin = new List<int>());
                bin.AddRange(g);
            }

            return big.Concat(bins).ToList();
        }

        static IEnumerable<List<int>> Chunk(List<int> items, int size)
        {
            for (var i = 0; i < items.Count; i += size)
            {
                yield return items.Skip(i).Take(size).ToList();
            }
        }

        static List<string> NamePages(List<List<int>> groups, List<DiagramTable> solution, Func<int, int, double> weight)
        {
            var names = new List<string>();
            foreach (var g in groups)
            {
                var top = g
                    .OrderByDescending(i => g.Sum(j => weight(i, j)))
                    .ThenBy(i => solution[i].DisplayName, StringComparer.OrdinalIgnoreCase)
                    .First();
                var name = solution[top].DisplayName;
                var candidate = name;
                for (var k = 2; names.Contains(candidate); k++) candidate = $"{name} ({k})";
                names.Add(candidate);
            }

            return names;
        }

        static DiagramPage BuildPage(DiagramModel model, string name, HashSet<string> full, Dictionary<string, string> homePage,
            ErdOptions options)
        {
            var page = new DiagramPage { Name = name };
            var tables = new Dictionary<string, PageTable>();
            foreach (var t in model.Tables.Where(t => full.Contains(t.Name)))
            {
                var pageTable = new PageTable(t, asStub: false);
                page.Tables.Add(pageTable);
                tables[t.Name] = pageTable;
            }

            // A page draws its own tables' lookups. Lookups into it from other pages are drawn on their own pages
            // unless asked for, so a hub's home page doesn't collect a line from every table that uses it.
            foreach (var e in model.Edges)
            {
                var childHere = full.Contains(e.ChildTable);
                var parentHere = full.Contains(e.ParentTable);
                string Name(string table) => model.Table(table)?.DisplayName ?? table;
                void Label(string text)
                {
                    var refs = tables[e.ChildTable].RowRefs;
                    if (!refs.TryGetValue(e.ChildRow, out var targets)) refs[e.ChildRow] = targets = new List<string>();
                    targets.Add(text);
                }

                if (childHere && e.ShortcutVia != null && !options.DrawShortcutLookups)
                {
                    Label(Name(e.ParentTable) + " (via " + Name(e.ShortcutVia) + ")");
                    continue;
                }

                if (childHere && !parentHere && e.Kind == EdgeKind.OneToMany && options.OffPageLinks == OffPageLinks.Labels)
                {
                    Label(Name(e.ParentTable));
                    continue;
                }

                if (childHere || (parentHere && (options.ShowIncomingFromOtherPages || e.Kind == EdgeKind.ManyToMany)))
                {
                    page.Edges.Add(e);
                }
            }

            var stubNames = page.Edges.SelectMany(e => new[] { e.ParentTable, e.ChildTable })
                .Where(t => !full.Contains(t))
                .Distinct()
                .OrderBy(t => t, StringComparer.Ordinal);
            foreach (var s in stubNames)
            {
                var table = model.Table(s);
                if (table == null) continue;
                var note = homePage.TryGetValue(s, out var home) ? "see " + home : table.InSolution ? null : "not in solution";
                page.Tables.Add(new PageTable(table, asStub: true, note));
            }

            return page;
        }

        static DiagramPage BuildOverview(DiagramModel model, List<List<int>> groups, List<DiagramTable> solution,
            List<string> pageNames, Dictionary<string, string> homePage)
        {
            var page = new DiagramPage { Name = "Overview", IsOverview = true };
            var areaOf = new Dictionary<string, string>();
            for (var p = 0; p < groups.Count; p++)
            {
                var area = new DiagramTable
                {
                    Name = "area:" + p,
                    DisplayName = pageNames[p],
                    InSolution = true,
                    Rows = groups[p]
                        .Select(i => solution[i])
                        .OrderBy(t => t.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .Select(t => new DiagramRow { Name = t.Name, DisplayName = t.DisplayName, Kind = RowKind.Column })
                        .ToList(),
                };
                page.Tables.Add(new PageTable(area, asStub: false));
                areaOf[pageNames[p]] = area.Name;
            }

            var counts = new Dictionary<(string child, string parent), int>();
            foreach (var e in model.Edges.Where(e => !e.IsSelf))
            {
                if (!homePage.TryGetValue(e.ChildTable, out var cp) || !homePage.TryGetValue(e.ParentTable, out var pp) || cp == pp) continue;
                var key = (areaOf[cp], areaOf[pp]);
                counts.TryGetValue(key, out var c);
                counts[key] = c + 1;
            }

            foreach (var kv in counts.OrderBy(kv => kv.Key.child).ThenBy(kv => kv.Key.parent))
            {
                page.Edges.Add(new DiagramEdge
                {
                    Id = kv.Key.child + "->" + kv.Key.parent,
                    Kind = EdgeKind.OneToMany,
                    ChildTable = kv.Key.child,
                    ParentTable = kv.Key.parent,
                    Label = kv.Value.ToString(),
                });
            }

            return page;
        }
    }
}
