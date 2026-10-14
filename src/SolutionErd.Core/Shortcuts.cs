using System;
using System.Collections.Generic;
using System.Linq;
using SolutionErd.Model;

namespace SolutionErd
{
    /// <summary>
    /// Finds denormalised lookups: a table that points at Structure and also at Project, when Structure itself
    /// has the same Project lookup. The direct line adds nothing a reader can't follow through Structure, so it
    /// is drawn as row text instead.
    /// </summary>
    public static class Shortcuts
    {
        const int MaxDepth = 3;

        public static void Mark(DiagramModel model)
        {
            var lookups = model.Edges
                .Where(e => e.Kind == EdgeKind.OneToMany && !e.IsSelf)
                .ToList();
            foreach (var e in lookups) e.ShortcutVia = null;

            var byChild = lookups
                .GroupBy(e => e.ChildTable)
                .ToDictionary(g => g.Key, g => g.ToList());
            var pairs = new HashSet<(string, string)>(lookups.Select(e => (e.ChildTable, e.ParentTable)));

            foreach (var siblings in byChild.Values)
            {
                // What each lookup explains: the other lookups whose target it also reaches.
                var explains = siblings.ToDictionary(via => via, via => siblings
                    .Where(e => e != via && e.ParentTable != via.ParentTable && e.ParentTable != via.ChildTable)
                    .Where(e => !(e.ParentRequired && !via.ParentRequired))
                    .Where(e => Hop(via.ChildTable, via.ParentTable, pairs)
                                && Reaches(via.ParentTable, e.ParentTable, e.ChildRow, byChild, pairs, new HashSet<string> { via.ChildTable }, 1))
                    .ToList());

                // Keep the lookup that explains the most open ones, hide what it explains, repeat. A hidden lookup
                // always names a lookup that stays drawn, so two lookups never hide each other.
                var open = new HashSet<DiagramEdge>(siblings);
                while (open.Count > 0)
                {
                    var keep = open
                        .OrderByDescending(v => explains[v].Count(open.Contains))
                        .ThenByDescending(v => v.ParentRequired)
                        .ThenBy(v => v.ChildRow, StringComparer.Ordinal)
                        .ThenBy(v => v.ParentTable, StringComparer.Ordinal)
                        .First();
                    open.Remove(keep);
                    foreach (var e in explains[keep].Where(open.Contains).ToList())
                    {
                        e.ShortcutVia = keep.ParentTable;
                        open.Remove(e);
                    }
                }
            }
        }

        /// <summary>
        /// Can <paramref name="from"/> reach <paramref name="target"/> by lookups, ending with a lookup column
        /// of the same name as the shortcut's?
        /// </summary>
        static bool Reaches(string from, string target, string column, Dictionary<string, List<DiagramEdge>> byChild,
            HashSet<(string, string)> pairs, HashSet<string> visited, int depth)
        {
            if (!visited.Add(from) || !byChild.TryGetValue(from, out var next)) return false;
            if (next.Any(e => e.ParentTable == target && e.ChildRow == column && Hop(from, target, pairs))) return true;
            if (depth >= MaxDepth) return false;
            return next.Any(e => e.ParentTable != target && Hop(from, e.ParentTable, pairs)
                                 && Reaches(e.ParentTable, target, column, byChild, pairs, visited, depth + 1));
        }

        /// <summary>
        /// A path only follows lookups that go up a hierarchy. When two tables look each other up (a project
        /// with a "last inspection" lookup, an inspection with a project lookup), neither direction counts.
        /// </summary>
        static bool Hop(string child, string parent, HashSet<(string, string)> pairs) => !pairs.Contains((parent, child));
    }
}
