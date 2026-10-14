using System.Collections.Generic;
using System.Linq;
using SolutionErd.Layout;
using SolutionErd.Model;

namespace SolutionErd
{
    /// <summary>Snapshot in, laid-out pages out: filter, paginate, lay out.</summary>
    public static class ErdBuilder
    {
        public static ErdDocument Build(ErdSnapshot snapshot, ErdOptions options, NoiseRules rules = null)
        {
            var model = NoiseFilter.Apply(snapshot, options, rules);
            var pages = Clusterer.Paginate(model, options, rules);
            return Build(model.Title, pages, options);
        }

        public static ErdDocument Build(string title, IEnumerable<DiagramPage> pages, ErdOptions options)
        {
            var engine = new LayoutEngine(options);
            return new ErdDocument
            {
                Title = title,
                Pages = pages.Select(engine.Layout).ToList(),
            };
        }
    }
}
