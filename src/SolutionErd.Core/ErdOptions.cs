namespace SolutionErd
{
    public enum ColumnMode { KeysAndLookups, KeysLookupsAndCustom, All }

    public enum RowLabel { DisplayAndLogical, Logical, Display }

    public enum LayoutDirection { LeftToRight, TopToBottom }

    /// <summary>How a lookup to a table that isn't drawn in full on the page is shown.</summary>
    public enum OffPageLinks
    {
        /// <summary>"→ Table" text on the lookup row, no line.</summary>
        Labels,

        /// <summary>A small name-only box for the target, with a line.</summary>
        Stubs,
    }

    public sealed class ErdOptions
    {
        public ColumnMode Columns { get; set; } = ColumnMode.KeysAndLookups;
        public RowLabel RowLabel { get; set; } = RowLabel.DisplayAndLogical;
        /// <summary>Show lookups to tables outside the solution (as labels or stubs, see <see cref="OffPageLinks"/>).</summary>
        public bool ShowOutsideTables { get; set; } = true;

        public OffPageLinks OffPageLinks { get; set; } = OffPageLinks.Labels;

        /// <summary>
        /// Draw lines for denormalised lookups too (a table pointing at Project when it already points at
        /// Structure, which points at Project). By default they are row text.
        /// </summary>
        public bool DrawShortcutLookups { get; set; }
        public bool ShowSystemRelationships { get; set; }

        /// <summary>Show out-of-solution tables whose lookups point at solution tables.</summary>
        public bool ShowExternalChildren { get; set; }

        /// <summary>Lookups with more targets than this only draw lines to solution tables.</summary>
        public int MaxPolymorphicTargets { get; set; } = 3;

        public bool SplitIntoPages { get; set; } = true;
        public int MaxTablesPerPage { get; set; } = 15;
        public int HubDegreeThreshold { get; set; } = 6;

        /// <summary>On split pages, also draw lookups from tables on other pages into this page's tables.</summary>
        public bool ShowIncomingFromOtherPages { get; set; }
        public LayoutDirection Direction { get; set; } = LayoutDirection.LeftToRight;

        public ErdOptions Clone() => (ErdOptions)MemberwiseClone();
    }
}
