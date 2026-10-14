using System.Collections.Generic;
using System.Linq;

namespace SolutionErd
{
    /// <summary>Deterministic Louvain community detection on a small undirected weighted graph.</summary>
    public static class Louvain
    {
        /// <param name="n">Node count; nodes are 0..n-1.</param>
        /// <param name="edges">Undirected weighted edges (a, b, w), a != b.</param>
        /// <returns>Communities as sorted node lists, ordered by their smallest node.</returns>
        public static List<List<int>> Detect(int n, IEnumerable<(int a, int b, double w)> edges, double resolution = 1.0)
        {
            var adj = new List<Dictionary<int, double>>();
            for (var i = 0; i < n; i++) adj.Add(new Dictionary<int, double>());
            foreach (var (a, b, w) in edges)
            {
                if (a == b) continue;
                Add(adj[a], b, w);
                Add(adj[b], a, w);
            }

            // members[x] = original nodes inside aggregated node x
            var members = Enumerable.Range(0, n).Select(i => new List<int> { i }).ToList();
            while (true)
            {
                var community = OneLevel(adj, resolution);
                var distinct = community.Distinct().Count();
                if (distinct == adj.Count) break;

                var renumber = new Dictionary<int, int>();
                foreach (var c in community)
                {
                    if (!renumber.ContainsKey(c)) renumber[c] = renumber.Count;
                }

                var nextAdj = Enumerable.Range(0, renumber.Count).Select(_ => new Dictionary<int, double>()).ToList();
                var nextMembers = Enumerable.Range(0, renumber.Count).Select(_ => new List<int>()).ToList();
                for (var i = 0; i < adj.Count; i++)
                {
                    var ci = renumber[community[i]];
                    nextMembers[ci].AddRange(members[i]);
                    foreach (var kv in adj[i])
                    {
                        Add(nextAdj[ci], renumber[community[kv.Key]], kv.Value);
                    }
                }

                adj = nextAdj;
                members = nextMembers;
                if (adj.Count == 1) break;
            }

            return members
                .Select(m => m.OrderBy(x => x).ToList())
                .OrderBy(m => m[0])
                .ToList();
        }

        static int[] OneLevel(List<Dictionary<int, double>> adj, double resolution)
        {
            var n = adj.Count;
            var community = Enumerable.Range(0, n).ToArray();
            var k = adj.Select(d => d.Values.Sum()).ToArray();
            var m2 = k.Sum();
            if (m2 <= 0) return community;

            var tot = (double[])k.Clone();
            var moved = true;
            var guard = 0;
            while (moved && guard++ < 100)
            {
                moved = false;
                for (var i = 0; i < n; i++)
                {
                    var own = community[i];
                    tot[own] -= k[i];

                    var links = new Dictionary<int, double>();
                    foreach (var kv in adj[i])
                    {
                        if (kv.Key == i) continue;
                        Add(links, community[kv.Key], kv.Value);
                    }

                    var best = own;
                    links.TryGetValue(own, out var ownLinks);
                    var bestGain = ownLinks - resolution * tot[own] * k[i] / m2;
                    foreach (var kv in links.OrderBy(x => x.Key))
                    {
                        var gain = kv.Value - resolution * tot[kv.Key] * k[i] / m2;
                        if (gain > bestGain + 1e-12)
                        {
                            bestGain = gain;
                            best = kv.Key;
                        }
                    }

                    tot[best] += k[i];
                    if (best != own)
                    {
                        community[i] = best;
                        moved = true;
                    }
                }
            }

            return community;
        }

        static void Add(Dictionary<int, double> d, int key, double w)
        {
            d.TryGetValue(key, out var current);
            d[key] = current + w;
        }
    }
}
