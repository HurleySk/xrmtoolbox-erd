namespace SolutionErd
{
    public enum ColumnMode { KeysAndLookups, KeysLookupsAndCustom, All }

    public enum RowLabel { DisplayAndLogical, Logical, Display }

    public enum LayoutDirection { LeftToRight, TopToBottom }

    public sealed class ErdOptions
    {
        public ColumnMode Columns { get; set; } = ColumnMode.KeysAndLookups;
        public RowLabel RowLabel { get; set; } = RowLabel.DisplayAndLogical;
        public bool ShowStubs { get; set; } = true;
        public bool ShowSystemRelationships { get; set; }

        /// <summary>Show out-of-solution tables whose lookups point at solution tables.</summary>
        public bool ShowExternalChildren { get; set; }

        /// <summary>Lookups with more targets than this only draw lines to solution tables.</summary>
        public int MaxPolymorphicTargets { get; set; } = 3;

        public bool SplitIntoPages { get; set; } = true;
        public int MaxTablesPerPage { get; set; } = 15;
        public int HubDegreeThreshold { get; set; } = 6;
        public LayoutDirection Direction { get; set; } = LayoutDirection.LeftToRight;

        public ErdOptions Clone() => (ErdOptions)MemberwiseClone();
    }
}
