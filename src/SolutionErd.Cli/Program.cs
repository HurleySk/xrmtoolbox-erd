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

  render <snapshot.json> <outDir> [options]
      Write <name>.drawio and one PNG per page, and print layout metrics.
      --columns keys|custom|all     rows shown in each table (default keys)
      --labels both|logical|display row text (default both)
      --direction lr|tb             layer direction (default lr)
      --single-page                 don't split into subject-area pages
      --max <n>                     max tables per page (default 15)
      --hub <n>                     hub degree threshold (default 6)
      --system                      show system relationships
      --no-stubs                    hide tables outside the solution
      --external                    show out-of-solution children
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
                    case "--no-stubs": options.ShowStubs = false; break;
                    case "--external": options.ShowExternalChildren = true; break;
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
                              $"bends {scores.Sum(s => s.Bends)}, box hits {scores.Sum(s => s.BoxHits)}, score {scores.Sum(s => s.Score):0.0}");
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
