using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using SolutionErd.Model;

namespace SolutionErdXrmToolBoxPlugin
{
    public sealed class SolutionSummary
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public bool IsManaged { get; set; }
        public string Publisher { get; set; }

        public override string ToString() => FriendlyName ?? UniqueName;
    }

    /// <summary>Reads a solution's tables, columns and relationships over the XrmToolBox connection.</summary>
    public sealed class MetadataReader
    {
        const int EntityComponentType = 1;
        const int ChunkSize = 100;

        static readonly string[] TableProperties =
        {
            "LogicalName", "SchemaName", "DisplayName", "PrimaryIdAttribute", "PrimaryNameAttribute",
            "IsCustomEntity", "IsIntersect", "IsActivity",
        };

        static readonly string[] ColumnProperties =
        {
            "LogicalName", "DisplayName", "AttributeType", "IsCustomAttribute", "IsPrimaryId", "IsPrimaryName",
            "RequiredLevel", "Targets", "AttributeOf",
        };

        static readonly string[] RelationshipProperties =
        {
            "SchemaName", "RelationshipType", "IsCustomRelationship", "ReferencedEntity", "ReferencedAttribute",
            "ReferencingEntity", "ReferencingAttribute", "Entity1LogicalName", "Entity2LogicalName", "IntersectEntityName",
        };

        readonly IOrganizationService service;

        public MetadataReader(IOrganizationService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public List<SolutionSummary> ListSolutions()
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename", "friendlyname", "version", "ismanaged", "publisherid"),
                Criteria = { Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) } },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) },
            };

            return RetrieveAll(query)
                .Select(e => new SolutionSummary
                {
                    Id = e.Id,
                    UniqueName = e.GetAttributeValue<string>("uniquename"),
                    FriendlyName = e.GetAttributeValue<string>("friendlyname"),
                    Version = e.GetAttributeValue<string>("version"),
                    IsManaged = e.GetAttributeValue<bool>("ismanaged"),
                    Publisher = e.GetAttributeValue<EntityReference>("publisherid")?.Name,
                })
                .ToList();
        }

        public ErdSnapshot Read(SolutionSummary solution, string environment, Action<string> progress = null)
        {
            progress?.Invoke("Reading solution components...");
            var ids = SolutionTableIds(solution.Id);

            progress?.Invoke($"Reading metadata for {ids.Count} tables...");
            var inSolution = RetrieveTables(ids.Cast<object>().ToArray(), "MetadataId", withDetails: true);

            var snapshot = new ErdSnapshot
            {
                Source = "dataverse",
                Environment = environment,
                SolutionName = solution.UniqueName,
                SolutionDisplayName = solution.FriendlyName ?? solution.UniqueName,
                CapturedUtc = DateTime.UtcNow,
            };

            var relationships = new Dictionary<string, RelationshipInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in inSolution)
            {
                snapshot.Tables.Add(ToTable(table, inSolutionTable: true));
                foreach (var rel in (table.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
                         .Concat(table.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0]))
                {
                    var info = ToRelationship(rel);
                    if (info != null && !relationships.ContainsKey(info.SchemaName)) relationships[info.SchemaName] = info;
                }

                foreach (var rel in table.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
                {
                    var info = ToRelationship(rel);
                    if (info != null && !relationships.ContainsKey(info.SchemaName)) relationships[info.SchemaName] = info;
                }
            }

            snapshot.Relationships = relationships.Values
                .OrderBy(r => r.SchemaName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Tables outside the solution that relationships point at: names and keys only.
            var known = new HashSet<string>(snapshot.Tables.Select(t => t.LogicalName), StringComparer.OrdinalIgnoreCase);
            var outside = snapshot.Relationships
                .SelectMany(r => new[] { r.ParentTable, r.ChildTable, r.IntersectTable })
                .Where(n => !string.IsNullOrEmpty(n) && !known.Contains(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (outside.Length > 0)
            {
                progress?.Invoke($"Reading names of {outside.Length} related tables...");
                foreach (var table in RetrieveTables(outside.Cast<object>().ToArray(), "LogicalName", withDetails: false))
                {
                    snapshot.Tables.Add(ToTable(table, inSolutionTable: false));
                }
            }

            snapshot.Tables = snapshot.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();
            return snapshot;
        }

        List<Guid> SolutionTableIds(Guid solutionId)
        {
            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, EntityComponentType),
                    },
                },
            };

            return RetrieveAll(query)
                .Select(e => e.GetAttributeValue<Guid>("objectid"))
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }

        List<EntityMetadata> RetrieveTables(object[] keys, string keyProperty, bool withDetails)
        {
            var result = new List<EntityMetadata>();
            for (var i = 0; i < keys.Length; i += ChunkSize)
            {
                var chunk = keys.Skip(i).Take(ChunkSize).ToArray();
                var properties = new MetadataPropertiesExpression(TableProperties);
                var query = new EntityQueryExpression
                {
                    Criteria = new MetadataFilterExpression(LogicalOperator.And)
                    {
                        Conditions = { new MetadataConditionExpression(keyProperty, MetadataConditionOperator.In, chunk) },
                    },
                    Properties = properties,
                };

                if (withDetails)
                {
                    properties.PropertyNames.AddRange(new[]
                    {
                        "Attributes", "OneToManyRelationships", "ManyToOneRelationships", "ManyToManyRelationships",
                    });
                    query.AttributeQuery = new AttributeQueryExpression { Properties = new MetadataPropertiesExpression(ColumnProperties) };
                    query.RelationshipQuery = new RelationshipQueryExpression { Properties = new MetadataPropertiesExpression(RelationshipProperties) };
                }

                var response = (RetrieveMetadataChangesResponse)service.Execute(new RetrieveMetadataChangesRequest
                {
                    Query = query,
                    ClientVersionStamp = null,
                });
                result.AddRange(response.EntityMetadata);
            }

            return result;
        }

        IEnumerable<Entity> RetrieveAll(QueryExpression query)
        {
            query.PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 };
            while (true)
            {
                var page = service.RetrieveMultiple(query);
                foreach (var e in page.Entities) yield return e;
                if (!page.MoreRecords) yield break;
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }
        }

        static TableInfo ToTable(EntityMetadata table, bool inSolutionTable)
        {
            var info = new TableInfo
            {
                LogicalName = table.LogicalName,
                SchemaName = table.SchemaName,
                DisplayName = Label(table.DisplayName) ?? table.LogicalName,
                PrimaryIdAttribute = table.PrimaryIdAttribute,
                PrimaryNameAttribute = table.PrimaryNameAttribute,
                InSolution = inSolutionTable,
                IsCustom = table.IsCustomEntity ?? false,
                IsIntersect = table.IsIntersect ?? false,
                IsActivity = table.IsActivity ?? false,
            };

            foreach (var column in table.Attributes ?? new AttributeMetadata[0])
            {
                // Skip helper columns (lookup name and type columns, yomi names) and virtual columns.
                if (!string.IsNullOrEmpty(column.AttributeOf) || column.AttributeType == AttributeTypeCode.Virtual) continue;
                var required = column.RequiredLevel?.Value;
                info.Columns.Add(new ColumnInfo
                {
                    LogicalName = column.LogicalName,
                    DisplayName = Label(column.DisplayName),
                    Type = column.AttributeType?.ToString(),
                    IsCustom = column.IsCustomAttribute ?? false,
                    IsPrimaryId = column.IsPrimaryId ?? false,
                    IsPrimaryName = column.IsPrimaryName ?? false,
                    IsRequired = required == AttributeRequiredLevel.ApplicationRequired || required == AttributeRequiredLevel.SystemRequired,
                    Targets = (column as LookupAttributeMetadata)?.Targets?.ToList(),
                });
            }

            info.Columns = info.Columns.OrderBy(c => c.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();
            return info;
        }

        static RelationshipInfo ToRelationship(OneToManyRelationshipMetadata rel)
        {
            if (string.IsNullOrEmpty(rel.SchemaName) || string.IsNullOrEmpty(rel.ReferencingEntity)) return null;
            return new RelationshipInfo
            {
                SchemaName = rel.SchemaName,
                Kind = RelationshipKind.OneToMany,
                ParentTable = rel.ReferencedEntity,
                ParentColumn = rel.ReferencedAttribute,
                ChildTable = rel.ReferencingEntity,
                ChildColumn = rel.ReferencingAttribute,
                IsCustom = rel.IsCustomRelationship ?? false,
            };
        }

        static RelationshipInfo ToRelationship(ManyToManyRelationshipMetadata rel)
        {
            if (string.IsNullOrEmpty(rel.SchemaName) || string.IsNullOrEmpty(rel.Entity1LogicalName)) return null;
            return new RelationshipInfo
            {
                SchemaName = rel.SchemaName,
                Kind = RelationshipKind.ManyToMany,
                ParentTable = rel.Entity1LogicalName,
                ChildTable = rel.Entity2LogicalName,
                IntersectTable = rel.IntersectEntityName,
                IsCustom = rel.IsCustomRelationship ?? false,
            };
        }

        static string Label(Label label)
        {
            var text = label?.UserLocalizedLabel?.Label ?? label?.LocalizedLabels?.FirstOrDefault()?.Label;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
