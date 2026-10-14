using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SolutionErd.Model
{
    /// <summary>Raw metadata for one solution, as read from Dataverse by the tool.</summary>
    public sealed class ErdSnapshot
    {
        public string Source { get; set; }
        public string Environment { get; set; }
        public string SolutionName { get; set; }
        public string SolutionDisplayName { get; set; }
        public DateTime CapturedUtc { get; set; }
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
        public List<RelationshipInfo> Relationships { get; set; } = new List<RelationshipInfo>();

        static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        public string ToJson() => JsonConvert.SerializeObject(this, JsonSettings);

        public static ErdSnapshot FromJson(string json) => JsonConvert.DeserializeObject<ErdSnapshot>(json, JsonSettings);

        public void Save(string path) => File.WriteAllText(path, ToJson());

        public static ErdSnapshot Load(string path) => FromJson(File.ReadAllText(path));
    }

    public sealed class TableInfo
    {
        public string LogicalName { get; set; }
        public string SchemaName { get; set; }
        public string DisplayName { get; set; }
        public string PrimaryIdAttribute { get; set; }
        public string PrimaryNameAttribute { get; set; }
        public bool InSolution { get; set; }
        public bool IsCustom { get; set; }
        public bool IsIntersect { get; set; }
        public bool IsActivity { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
    }

    public sealed class ColumnInfo
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }

        /// <summary>Dataverse AttributeType, e.g. Lookup, Customer, Owner, String, Picklist.</summary>
        public string Type { get; set; }

        public bool IsCustom { get; set; }
        public bool IsPrimaryId { get; set; }
        public bool IsPrimaryName { get; set; }
        public bool IsRequired { get; set; }
        public List<string> Targets { get; set; }

        [JsonIgnore]
        public bool IsLookup => Type == "Lookup" || Type == "Customer" || Type == "Owner";
    }

    public enum RelationshipKind { OneToMany, ManyToMany }

    /// <summary>
    /// 1:N: Parent is the referenced table, Child the referencing table holding the lookup ChildColumn.
    /// N:N: Parent and Child are the two tables; IntersectTable holds the join.
    /// </summary>
    public sealed class RelationshipInfo
    {
        public string SchemaName { get; set; }
        public RelationshipKind Kind { get; set; }
        public string ParentTable { get; set; }
        public string ParentColumn { get; set; }
        public string ChildTable { get; set; }
        public string ChildColumn { get; set; }
        public string IntersectTable { get; set; }
        public bool IsCustom { get; set; }
    }
}
