using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using SolutionErd.Model;
using SolutionErdXrmToolBoxPlugin;
using Xunit;

namespace SolutionErd.Tests
{
    public class MetadataReaderTests
    {
        static readonly Guid SolutionId = Guid.NewGuid();

        [Fact]
        public void ListsVisibleSolutions()
        {
            var service = Org();

            var solutions = new MetadataReader(service).ListSolutions();

            var solution = Assert.Single(solutions);
            Assert.Equal("contosoprojects", solution.UniqueName);
            Assert.Equal("Contoso Projects", solution.FriendlyName);
            Assert.Equal("Contoso", solution.Publisher);
            var query = (QueryExpression)service.Queries.First();
            Assert.Contains(query.Criteria.Conditions, c => c.AttributeName == "isvisible");
        }

        [Fact]
        public void ReadsSolutionTablesColumnsAndRelationships()
        {
            var snapshot = new MetadataReader(Org()).Read(Solution(), "dev");

            var task = snapshot.Tables.Single(t => t.LogicalName == "cp_task");
            Assert.True(task.InSolution);
            Assert.True(task.IsCustom);
            Assert.Equal("Task", task.DisplayName);
            Assert.Equal("cp_taskid", task.PrimaryIdAttribute);
            var lookup = task.Columns.Single(c => c.LogicalName == "cp_projectid");
            Assert.Equal("Lookup", lookup.Type);
            Assert.True(lookup.IsRequired);
            Assert.True(lookup.IsCustom);
            Assert.Equal(new[] { "cp_project" }, lookup.Targets);
            Assert.DoesNotContain(task.Columns, c => c.LogicalName == "cp_projectidname");

            var rel = snapshot.Relationships.Single(r => r.SchemaName == "cp_project_cp_task");
            Assert.Equal(RelationshipKind.OneToMany, rel.Kind);
            Assert.Equal("cp_project", rel.ParentTable);
            Assert.Equal("cp_task", rel.ChildTable);
            Assert.Equal("cp_projectid", rel.ChildColumn);
            Assert.Equal(1, snapshot.Relationships.Count(r => r.SchemaName == "cp_project_cp_task"));
            Assert.Equal("dev", snapshot.Environment);
        }

        [Fact]
        public void ReadsNamesOfRelatedTablesOutsideTheSolution()
        {
            var snapshot = new MetadataReader(Org()).Read(Solution(), "dev");

            var user = snapshot.Tables.Single(t => t.LogicalName == "systemuser");
            Assert.False(user.InSolution);
            Assert.Equal("User", user.DisplayName);
            Assert.Empty(user.Columns);
            var intersect = snapshot.Tables.Single(t => t.LogicalName == "cp_task_contact");
            Assert.True(intersect.IsIntersect);
            Assert.Contains(snapshot.Relationships, r => r.Kind == RelationshipKind.ManyToMany && r.IntersectTable == "cp_task_contact");
        }

        [Fact]
        public void SnapshotDrawsEndToEnd()
        {
            var snapshot = new MetadataReader(Org()).Read(Solution(), "dev");

            var page = ErdBuilder.Build(snapshot, new ErdOptions()).Pages.Single();

            Assert.Contains(page.Edges, e => e.FromRow == "cp_projectid");
            Assert.Equal("→ User", page.Boxes.Single(b => b.Name == "cp_task").Row("cp_ownerresourceid").Ref);
            Assert.DoesNotContain(page.Boxes, b => b.Name == "systemuser");
        }

        [Fact]
        public void SnapshotRoundTripsThroughJson()
        {
            var snapshot = new MetadataReader(Org()).Read(Solution(), "dev");

            var copy = ErdSnapshot.FromJson(snapshot.ToJson());

            Assert.Equal(snapshot.ToJson(), copy.ToJson());
        }

        static SolutionSummary Solution() => new SolutionSummary { Id = SolutionId, UniqueName = "contosoprojects", FriendlyName = "Contoso Projects" };

        static FakeOrg Org()
        {
            var project = Table("cp_project", "Project", custom: true,
                Column<StringAttributeMetadata>("cp_name", "Name", primaryName: true),
                Column<AttributeMetadata>("cp_projectid", "Project", primaryId: true, type: AttributeTypeCode.Uniqueidentifier));
            var task = Table("cp_task", "Task", custom: true,
                Column<AttributeMetadata>("cp_taskid", "Task", primaryId: true, type: AttributeTypeCode.Uniqueidentifier),
                Column<StringAttributeMetadata>("cp_name", "Name", primaryName: true),
                Lookup("cp_projectid", "Project", required: true, "cp_project"),
                Lookup("cp_ownerresourceid", "Owner Resource", required: false, "systemuser"),
                Column<StringAttributeMetadata>("cp_projectidname", "Project Name", attributeOf: "cp_projectid"));
            var toProject = OneToMany("cp_project_cp_task", "cp_project", "cp_task", "cp_projectid", custom: true);
            var toUser = OneToMany("systemuser_cp_task", "systemuser", "cp_task", "cp_ownerresourceid", custom: true);
            var createdBy = OneToMany("lk_cp_task_createdby", "systemuser", "cp_task", "createdby", custom: false);
            var manyToMany = new ManyToManyRelationshipMetadata
            {
                SchemaName = "cp_task_contact",
                Entity1LogicalName = "cp_task",
                Entity2LogicalName = "contact",
                IntersectEntityName = "cp_task_contact",
                IsCustomRelationship = true,
            };
            Set(project, "OneToManyRelationships", new[] { toProject });
            Set(project, "ManyToOneRelationships", new OneToManyRelationshipMetadata[0]);
            Set(project, "ManyToManyRelationships", new ManyToManyRelationshipMetadata[0]);
            Set(task, "OneToManyRelationships", new OneToManyRelationshipMetadata[0]);
            Set(task, "ManyToOneRelationships", new[] { toProject, toUser, createdBy });
            Set(task, "ManyToManyRelationships", new[] { manyToMany });

            var intersect = Table("cp_task_contact", "cp_task_contact", custom: true);
            Set(intersect, "IsIntersect", (bool?)true);

            var org = new FakeOrg
            {
                Metadata =
                {
                    project, task, intersect,
                    Table("systemuser", "User", custom: false),
                    Table("contact", "Contact", custom: false),
                    Table("account", "Account", custom: false),
                },
                Components = { project.MetadataId.Value, task.MetadataId.Value },
            };
            org.Solutions.Add(new Entity("solution", SolutionId)
            {
                ["uniquename"] = "contosoprojects",
                ["friendlyname"] = "Contoso Projects",
                ["version"] = "1.0.0.0",
                ["ismanaged"] = false,
                ["publisherid"] = new EntityReference("publisher", Guid.NewGuid()) { Name = "Contoso" },
            });
            return org;
        }

        static EntityMetadata Table(string name, string display, bool custom, params AttributeMetadata[] columns)
        {
            var table = new EntityMetadata { LogicalName = name, SchemaName = name, DisplayName = new Label(display, 1033), MetadataId = Guid.NewGuid() };
            Set(table, "IsCustomEntity", (bool?)custom);
            Set(table, "IsIntersect", (bool?)false);
            Set(table, "IsActivity", (bool?)false);
            Set(table, "PrimaryIdAttribute", name + "id");
            Set(table, "PrimaryNameAttribute", custom ? "cp_name" : "fullname");
            Set(table, "Attributes", columns);
            return table;
        }

        static AttributeMetadata Column<T>(string name, string display, bool primaryId = false, bool primaryName = false,
            AttributeTypeCode? type = null, string attributeOf = null) where T : AttributeMetadata, new()
        {
            var column = new T { LogicalName = name, SchemaName = name, DisplayName = new Label(display, 1033) };
            if (type != null) Set(column, "AttributeType", type);
            Set(column, "IsCustomAttribute", (bool?)true);
            Set(column, "IsPrimaryId", (bool?)primaryId);
            Set(column, "IsPrimaryName", (bool?)primaryName);
            if (attributeOf != null) Set(column, "AttributeOf", attributeOf);
            column.RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None);
            return column;
        }

        static AttributeMetadata Lookup(string name, string display, bool required, params string[] targets)
        {
            var column = (LookupAttributeMetadata)Column<LookupAttributeMetadata>(name, display);
            column.Targets = targets;
            column.RequiredLevel = new AttributeRequiredLevelManagedProperty(required ? AttributeRequiredLevel.ApplicationRequired : AttributeRequiredLevel.None);
            return column;
        }

        static OneToManyRelationshipMetadata OneToMany(string schema, string parent, string child, string column, bool custom) =>
            new OneToManyRelationshipMetadata
            {
                SchemaName = schema,
                ReferencedEntity = parent,
                ReferencedAttribute = parent + "id",
                ReferencingEntity = child,
                ReferencingAttribute = column,
                IsCustomRelationship = custom,
            };

        static void Set(object target, string property, object value) =>
            target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(target, value);

        /// <summary>Answers the solution, solution component and metadata calls the reader makes.</summary>
        sealed class FakeOrg : IOrganizationService
        {
            public List<EntityMetadata> Metadata { get; } = new List<EntityMetadata>();
            public List<Guid> Components { get; } = new List<Guid>();
            public List<Entity> Solutions { get; } = new List<Entity>();
            public List<QueryBase> Queries { get; } = new List<QueryBase>();

            public EntityCollection RetrieveMultiple(QueryBase query)
            {
                Queries.Add(query);
                var q = (QueryExpression)query;
                switch (q.EntityName)
                {
                    case "solution":
                        return new EntityCollection(Solutions);
                    case "solutioncomponent":
                        Assert.Contains(q.Criteria.Conditions, c => c.AttributeName == "componenttype" && (int)c.Values[0] == 1);
                        return new EntityCollection(Components.Select(id => new Entity("solutioncomponent", Guid.NewGuid()) { ["objectid"] = id }).ToList());
                    default:
                        throw new NotSupportedException(q.EntityName);
                }
            }

            public OrganizationResponse Execute(OrganizationRequest request)
            {
                var query = ((RetrieveMetadataChangesRequest)request).Query;
                var condition = query.Criteria.Conditions.Single();
                var keys = ((object[])condition.Value).Select(k => k.ToString()).ToList();
                var details = query.Properties.PropertyNames.Contains("Attributes");
                var tables = Metadata
                    .Where(m => keys.Contains(condition.PropertyName == "MetadataId" ? m.MetadataId.ToString() : m.LogicalName))
                    .Select(m => details ? m : Bare(m))
                    .ToList();
                var collection = new EntityMetadataCollection();
                collection.AddRange(tables);
                var response = new RetrieveMetadataChangesResponse();
                response.Results["EntityMetadata"] = collection;
                return response;
            }

            static EntityMetadata Bare(EntityMetadata m)
            {
                var copy = new EntityMetadata { LogicalName = m.LogicalName, SchemaName = m.SchemaName, DisplayName = m.DisplayName, MetadataId = m.MetadataId };
                Set(copy, "IsCustomEntity", m.IsCustomEntity);
                Set(copy, "IsIntersect", m.IsIntersect);
                Set(copy, "IsActivity", m.IsActivity);
                Set(copy, "PrimaryIdAttribute", m.PrimaryIdAttribute);
                Set(copy, "PrimaryNameAttribute", m.PrimaryNameAttribute);
                return copy;
            }

            public Guid Create(Entity entity) => throw new NotSupportedException();
            public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotSupportedException();
            public void Update(Entity entity) => throw new NotSupportedException();
            public void Delete(string entityName, Guid id) => throw new NotSupportedException();
            public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
            public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
        }
    }
}
