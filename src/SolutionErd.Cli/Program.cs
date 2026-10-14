using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SolutionErd.Layout;
using SolutionErd.Model;
using SolutionErd.Render;
using SolutionErd.Writers;

namespace SolutionErd.Cli
{
    static class Program
    {
        const string Usage = @"solution-erd: dev tool for the Solution ERD Builder plugin

  stats <snapshot.json>
      Per page: full tables, stubs, and lines by kind (inside the page, to stubs, shortcuts).

  render <snapshot.json> <outDir> [options]
      Write <name>.drawio and one PNG per page, and print layout metrics.
      --columns keys|custom|all     rows shown in each table (default keys)
      --labels both|logical|display row text (default both)
      --direction lr|tb             layer direction (default lr)
      --single-page                 don't split into subject-area pages
      --max <n>                     max tables per page (default 15)
      --hub <n>                     hub degree threshold (default 6)
      --system                      show system relationships
      --stubs                       draw off-page lookup targets as boxes with lines, not row labels
      --shortcuts                   draw lines for denormalised lookups too
      --no-outside                  hide lookups to tables outside the solution
      --external                    show out-of-solution children
      --incoming                    on split pages, also draw lookups from other pages
      --overview layered|routed|straight   overview page layout (experiment 005)
      --scale <f>                   PNG scale (default 1)
      --no-png                      skip PNGs
      --name <name>                 output file name (default: snapshot file name)";

        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0) return Fail(Usage);
                switch (args[0])
                {
                    case "render" when args.Length >= 3:
                        return Render(args[1], args[2], args.Skip(3).ToList());
                    case "stats" when args.Length >= 2:
                        return Stats(args[1]);
                    default:
                        return Fail(Usage);
                }
            }
            catch (Exception ex)
            {
                return Fail(ex.ToString());
            }
        }

        static int Render(string snapshotPath, string outDir, List<string> flags)
        {
            var options = new ErdOptions();
            var scale = 1f;
            var png = true;
            var name = Path.GetFileNameWithoutExtension(snapshotPath);
            for (var i = 0; i < flags.Count; i++)
            {
                string Next() => i + 1 < flags.Count ? flags[++i] : throw new ArgumentException("missing value for " + flags[i]);
                switch (flags[i])
                {
                    case "--columns":
                        options.Columns = Next() switch { "custom" => ColumnMode.KeysLookupsAndCustom, "all" => ColumnMode.All, _ => ColumnMode.KeysAndLookups };
                        break;
                    case "--labels":
                        options.RowLabel = Next() switch { "logical" => RowLabel.Logical, "display" => RowLabel.Display, _ => RowLabel.DisplayAndLogical };
                        break;
                    case "--direction":
                        options.Direction = Next() == "tb" ? LayoutDirection.TopToBottom : LayoutDirection.LeftToRight;
                        break;
                    case "--single-page": options.SplitIntoPages = false; break;
                    case "--max": options.MaxTablesPerPage = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--hub": options.HubDegreeThreshold = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--system": options.ShowSystemRelationships = true; break;
                    case "--stubs": options.OffPageLinks = OffPageLinks.Stubs; break;
                    case "--shortcuts": options.DrawShortcutLookups = true; break;
                    case "--no-outside": options.ShowOutsideTables = false; break;
                    case "--external": options.ShowExternalChildren = true; break;
                    case "--incoming": options.ShowIncomingFromOtherPages = true; break;
                    case "--overview":
                        var style = Next();
                        options.OverviewStyle = style == "layered" ? OverviewStyle.Layered
                            : style == "routed" ? OverviewStyle.SpreadRouted : OverviewStyle.SpreadStraight;
                        break;
                    case "--scale": scale = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--no-png": png = false; break;
                    case "--name": name = Next(); break;
                    default: throw new ArgumentException("unknown option " + flags[i]);
                }
            }

            var snapshot = ErdSnapshot.Load(snapshotPath);
            var document = ErdBuilder.Build(snapshot, options);
            Directory.CreateDirectory(outDir);
            DrawioWriter.Write(document, Path.Combine(outDir, name + ".drawio"));

            var scores = new List<LayoutScore>();
            for (var p = 0; p < document.Pages.Count; p++)
            {
                var page = document.Pages[p];
                var score = LayoutMetrics.Measure(page);
                scores.Add(score);
                Console.WriteLine(score);
                if (png) PreviewRenderer.SavePng(page, Path.Combine(outDir, $"{name}-{p:00}-{Slug(page.Name)}.png"), scale);
            }

            Console.WriteLine($"TOTAL: pages {scores.Count}, edges {scores.Sum(s => s.Edges)}, crossings {scores.Sum(s => s.Crossings)}, " +
                              $"bends {scores.Sum(s => s.Bends)}, box hits {scores.Sum(s => s.BoxHits)}, overlap {scores.Sum(s => s.Overlap):0}, score {scores.Sum(s => s.Score):0.0}");
            return 0;
        }

        static int Stats(string snapshotPath)
        {
            var options = new ErdOptions();
            var model = NoiseFilter.Apply(ErdSnapshot.Load(snapshotPath), options);
            var pages = Clusterer.Paginate(model, options);

            // A lookup is a shortcut when its target can also be reached through the table's other lookups.
            var parents = model.Edges.Where(e => e.Kind == EdgeKind.OneToMany && !e.IsSelf)
                .GroupBy(e => e.ChildTable)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ParentTable).Distinct().ToList());
            bool Reachable(string from, string to, string skipFirst)
            {
                var seen = new HashSet<string> { from };
                var stack = new Stack<string>((parents.TryGetValue(from, out var p) ? p : new List<string>()).Where(x => x != skipFirst));
                while (stack.Count > 0)
                {
                    var t = stack.Pop();
                    if (t == to) return true;
                    if (!seen.Add(t)) continue;
                    if (parents.TryGetValue(t, out var next)) foreach (var n in next) stack.Push(n);
                }

                return false;
            }

            var required = model.Edges.Where(e => e.Kind == EdgeKind.OneToMany && !e.IsSelf && e.ParentRequired)
                .GroupBy(e => e.ChildTable)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ParentTable).Distinct().ToList());
            bool ViaRequired(DiagramEdge edge)
            {
                // Is the parent reachable through a chain of required lookups that starts with another lookup?
                var seen = new HashSet<string>();
                var stack = new Stack<string>((required.TryGetValue(edge.ChildTable, out var r) ? r : new List<string>()).Where(x => x != edge.ParentTable));
                while (stack.Count > 0)
                {
                    var t = stack.Pop();
                    if (t == edge.ParentTable) return true;
                    if (!seen.Add(t)) continue;
                    if (required.TryGetValue(t, out var next)) foreach (var n in next) stack.Push(n);
                }

                return false;
            }

            var oneToMany = model.Edges.Where(e => e.Kind == EdgeKind.OneToMany && !e.IsSelf).ToList();
            Console.WriteLine($"lookups {oneToMany.Count}, required {oneToMany.Count(e => e.ParentRequired)}, shortcuts via required chain {oneToMany.Count(ViaRequired)}, " +
                              $"inside solution {oneToMany.Count(e => model.Table(e.ParentTable).InSolution && model.Table(e.ChildTable).InSolution)}");
            Console.WriteLine($"model: tables {model.Tables.Count(t => t.InSolution)}, outside {model.Tables.Count(t => !t.InSolution)}, edges {model.Edges.Count}, " +
                              $"shortcuts {model.Edges.Count(e => e.Kind == EdgeKind.OneToMany && !e.IsSelf && Reachable(e.ChildTable, e.ParentTable, e.ParentTable))}");
            foreach (var page in pages.Where(p => !p.IsOverview))
            {
                var full = new HashSet<string>(page.Tables.Where(t => !t.AsStub).Select(t => t.Table.Name));
                var inside = page.Edges.Count(e => full.Contains(e.ChildTable) && full.Contains(e.ParentTable));
                var toStub = page.Edges.Count(e => full.Contains(e.ChildTable) && !full.Contains(e.ParentTable));
                var fromStub = page.Edges.Count(e => !full.Contains(e.ChildTable));
                var shortcuts = page.Edges.Count(e => e.Kind == EdgeKind.OneToMany && !e.IsSelf && Reachable(e.ChildTable, e.ParentTable, e.ParentTable));
                var parallel = page.Edges.GroupBy(e => (e.ChildTable, e.ParentTable)).Sum(g => g.Count() - 1);
                var stubFanIn = page.Edges.Where(e => !full.Contains(e.ParentTable)).GroupBy(e => e.ParentTable).Select(g => g.Count()).DefaultIfEmpty(0).Max();
                Console.WriteLine($"full {full.Count}, stubs {page.Tables.Count(t => t.AsStub)}, edges {page.Edges.Count}: inside {inside}, to stubs {toStub}, " +
                                  $"from stubs {fromStub}, shortcuts {shortcuts}, parallel {parallel}, max stub fan-in {stubFanIn}");
            }

            return 0;
        }

        static string Slug(string s) =>
            new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');

        static int Fail(string message)
        {
            Console.Error.WriteLine(message);
            return 1;
        }
    }
}
