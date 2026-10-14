using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SolutionErd
{
    /// <summary>Which relationships count as platform noise. Defaults ship as noise-rules.json.</summary>
    public sealed class NoiseRules
    {
        public List<string> SystemLookupColumns { get; set; } = new List<string>();
        public List<string> SystemTables { get; set; } = new List<string>();
        public List<string> CoreHubTables { get; set; } = new List<string>();

        HashSet<string> systemColumns, systemTables, coreHubs;

        public bool IsSystemColumn(string column) =>
            column != null && (systemColumns ?? (systemColumns = Set(SystemLookupColumns))).Contains(column);

        public bool IsSystemTable(string table) =>
            table != null && (systemTables ?? (systemTables = Set(SystemTables))).Contains(table);

        public bool IsCoreHub(string table) =>
            table != null && (coreHubs ?? (coreHubs = Set(CoreHubTables))).Contains(table);

        static HashSet<string> Set(IEnumerable<string> items) =>
            new HashSet<string>(items ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        static readonly Lazy<NoiseRules> DefaultRules = new Lazy<NoiseRules>(LoadDefault);

        public static NoiseRules Default => DefaultRules.Value;

        public static NoiseRules FromJson(string json) =>
            JsonConvert.DeserializeObject<NoiseRules>(json, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });

        static NoiseRules LoadDefault()
        {
            var assembly = typeof(NoiseRules).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith("noise-rules.json", StringComparison.Ordinal));
            using (var stream = assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
            {
                return FromJson(reader.ReadToEnd());
            }
        }
    }
}
