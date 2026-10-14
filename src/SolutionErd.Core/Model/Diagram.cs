using System.Collections.Generic;
using System.Linq;

namespace SolutionErd.Model
{
    public enum RowKind { PrimaryKey, PrimaryName, Lookup, Column }

    public sealed class DiagramRow
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public RowKind Kind { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
    }

    public sealed class DiagramTable
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool InSolution { get; set; }
        public List<DiagramRow> Rows { get; set; } = new List<DiagramRow>();
    }

    public enum EdgeKind { OneToMany, ManyToMany }

    /// <summary>A line from the child's lookup row to the parent's key row (or box, for stubs).</summary>
    public sealed class DiagramEdge
    {
        public string Id { get; set; }
        public EdgeKind Kind { get; set; }
        public string ParentTable { get; set; }
        public string ParentRow { get; set; }
        public string ChildTable { get; set; }
        public string ChildRow { get; set; }
        public bool ParentRequired { get; set; }
        public string Label { get; set; }

        /// <summary>Set when the parent is also reached through the lookup to this table (see <see cref="Shortcuts"/>).</summary>
        public string ShortcutVia { get; set; }

        public bool IsSelf => ParentTable == ChildTable;
    }

    public sealed class DiagramModel
    {
        public string Title { get; set; }
        public List<DiagramTable> Tables { get; set; } = new List<DiagramTable>();
        public List<DiagramEdge> Edges { get; set; } = new List<DiagramEdge>();

        public DiagramTable Table(string name) => Tables.FirstOrDefault(t => t.Name == name);
    }

    public sealed class PageTable
    {
        public PageTable(DiagramTable table, bool asStub, string note = null)
        {
            Table = table;
            AsStub = asStub;
            Note = note;
        }

        public DiagramTable Table { get; }
        public bool AsStub { get; }

        /// <summary>Shown under a stub's title, e.g. the page that holds the full table.</summary>
        public string Note { get; }

        /// <summary>Lookup rows drawn as text rather than lines: row name to "Target" or "Target (via Other)".</summary>
        public Dictionary<string, List<string>> RowRefs { get; } = new Dictionary<string, List<string>>();
    }

    public sealed class DiagramPage
    {
        public string Name { get; set; }
        public bool IsOverview { get; set; }
        public List<PageTable> Tables { get; set; } = new List<PageTable>();
        public List<DiagramEdge> Edges { get; set; } = new List<DiagramEdge>();
    }
}
