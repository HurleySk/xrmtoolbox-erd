"""Writes the synthetic metadata snapshots in fixtures/synthetic.

The solutions are made up (publisher prefix cp_, "Contoso Projects") but shaped like real ones:
denormalised lookups (many tables point at project and site), shared config tables, core tables
such as account and contact, system lookups on every table, a self-reference, a customer lookup,
N:N relationships and platform tables that point back at solution tables.

Run from the repo root:  python scripts/make_synthetic_fixtures.py
"""

import json
import os

PREFIX = "cp_"
CAPTURED = "2026-01-01T00:00:00Z"

SYSTEM_LOOKUPS = [
    ("createdby", "Created By", "Lookup", ["systemuser"]),
    ("createdonbehalfby", "Created By (Delegate)", "Lookup", ["systemuser"]),
    ("modifiedby", "Modified By", "Lookup", ["systemuser"]),
    ("modifiedonbehalfby", "Modified By (Delegate)", "Lookup", ["systemuser"]),
    ("ownerid", "Owner", "Owner", ["systemuser", "team"]),
    ("owninguser", "Owning User", "Lookup", ["systemuser"]),
    ("owningteam", "Owning Team", "Lookup", ["team"]),
    ("owningbusinessunit", "Owning Business Unit", "Lookup", ["businessunit"]),
    ("transactioncurrencyid", "Currency", "Lookup", ["transactioncurrency"]),
]

OOB = {
    "account": ("Account", "accountid", "name"),
    "contact": ("Contact", "contactid", "fullname"),
    "systemuser": ("User", "systemuserid", "fullname"),
    "team": ("Team", "teamid", "name"),
    "businessunit": ("Business Unit", "businessunitid", "name"),
    "transactioncurrency": ("Currency", "transactioncurrencyid", "currencyname"),
    "annotation": ("Note", "annotationid", "subject"),
    "activitypointer": ("Activity", "activityid", "subject"),
    "connection": ("Connection", "connectionid", "name"),
}


def title(name):
    return " ".join(w.capitalize() for w in name.replace(PREFIX, "").split("_"))


class Solution:
    def __init__(self, name, display):
        self.name = name
        self.display = display
        self.tables = {}
        self.relationships = []

    def oob(self, logical, in_solution=False):
        display, pk, primary = OOB[logical]
        if logical in self.tables:
            self.tables[logical]["inSolution"] |= in_solution
            return
        self.tables[logical] = {
            "logicalName": logical,
            "schemaName": logical.capitalize(),
            "displayName": display,
            "primaryIdAttribute": pk,
            "primaryNameAttribute": primary,
            "inSolution": in_solution,
            "isCustom": False,
            "isIntersect": False,
            "isActivity": logical == "activitypointer",
            "columns": [
                {"logicalName": pk, "displayName": display, "type": "Uniqueidentifier", "isPrimaryId": True},
                {"logicalName": primary, "displayName": "Name", "type": "String", "isPrimaryName": True},
            ],
        }

    def table(self, short, display=None, columns=(), lookups=(), activity=False):
        """lookups: (column short name, display, target, required); target may be a list (customer)."""
        logical = PREFIX + short
        pk = logical + "id"
        cols = [
            {"logicalName": pk, "displayName": display or title(logical), "type": "Uniqueidentifier", "isPrimaryId": True},
            {"logicalName": PREFIX + "name", "displayName": "Name", "type": "String", "isCustom": True,
             "isPrimaryName": True, "isRequired": True},
        ]
        for col, col_display, kind in columns:
            cols.append({"logicalName": PREFIX + col, "displayName": col_display, "type": kind, "isCustom": True})

        for col, col_display, kind, targets in SYSTEM_LOOKUPS:
            cols.append({"logicalName": col, "displayName": col_display, "type": kind, "targets": targets})
            for target in targets:
                self.oob(target)
                self.relationship(target, logical, col, custom=False)

        for col, col_display, target, required in lookups:
            targets = target if isinstance(target, list) else [target]
            targets = [t if t in OOB else PREFIX + t for t in targets]
            kind = "Customer" if len(targets) > 1 else "Lookup"
            column = {"logicalName": PREFIX + col, "displayName": col_display, "type": kind, "isCustom": True,
                      "targets": targets}
            if required:
                column["isRequired"] = True
            cols.append(column)
            for t in targets:
                if t in OOB:
                    self.oob(t)
                self.relationship(t, logical, PREFIX + col, custom=True)

        self.tables[logical] = {
            "logicalName": logical,
            "schemaName": logical,
            "displayName": display or title(logical),
            "primaryIdAttribute": pk,
            "primaryNameAttribute": PREFIX + "name",
            "inSolution": True,
            "isCustom": True,
            "isIntersect": False,
            "isActivity": activity,
            "columns": cols,
        }

        # Platform tables that point back at every table: notes, activities, connections.
        self.oob("annotation")
        self.oob("activitypointer")
        self.oob("connection")
        self.relationship(logical, "annotation", "objectid", custom=False)
        self.relationship(logical, "activitypointer", "regardingobjectid", custom=False)
        self.relationship(logical, "connection", "record1id", custom=False)

    def custom_lookup_on_oob(self, oob_table, col, col_display, target):
        """A custom lookup added to a core table that is part of the solution."""
        target = PREFIX + target
        self.tables[oob_table]["columns"].append(
            {"logicalName": PREFIX + col, "displayName": col_display, "type": "Lookup", "isCustom": True,
             "targets": [target]})
        self.relationship(target, oob_table, PREFIX + col, custom=True)

    def relationship(self, parent, child, column, custom):
        self.relationships.append({
            "schemaName": f"{parent}_{child}_{column}",
            "kind": "OneToMany",
            "parentTable": parent,
            "parentColumn": OOB[parent][1] if parent in OOB else parent + "id",
            "childTable": child,
            "childColumn": column,
            "isCustom": custom,
        })

    def many_to_many(self, a, b):
        a = a if a in OOB else PREFIX + a
        b = b if b in OOB else PREFIX + b
        intersect = f"{PREFIX}{a.replace(PREFIX, '')}_{b.replace(PREFIX, '')}"
        self.tables[intersect] = {
            "logicalName": intersect,
            "schemaName": intersect,
            "displayName": intersect,
            "primaryIdAttribute": intersect + "id",
            "inSolution": True,
            "isCustom": True,
            "isIntersect": True,
            "isActivity": False,
            "columns": [],
        }
        self.relationships.append({
            "schemaName": intersect,
            "kind": "ManyToMany",
            "parentTable": a,
            "childTable": b,
            "intersectTable": intersect,
            "isCustom": True,
        })

    def write(self, path):
        snapshot = {
            "source": "synthetic",
            "solutionName": self.name,
            "solutionDisplayName": self.display,
            "capturedUtc": CAPTURED,
            "tables": [self.tables[k] for k in sorted(self.tables)],
            "relationships": sorted(self.relationships, key=lambda r: r["schemaName"]),
        }
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            json.dump(snapshot, f, indent=2)
            f.write("\n")


def project_delivery():
    """About 40 tables in five loose areas, with heavy cross-linking through project and site."""
    s = Solution("ContosoProjectDelivery", "Contoso Project Delivery")
    s.oob("account", in_solution=True)
    s.oob("contact", in_solution=True)

    # Config
    s.table("region", lookups=[("parentregionid", "Parent Region", "region", False)])
    s.table("category", columns=[("code", "Code", "String")])
    s.table("fundingsource", "Funding Source", lookups=[("sponsorid", "Sponsor", "account", False)])

    # Portfolio
    s.table("program", lookups=[("regionid", "Region", "region", True), ("sponsorid", "Sponsor", "account", False),
                                ("directorid", "Director", "systemuser", False)])
    s.table("project", columns=[("startdate", "Start Date", "DateTime"), ("status", "Status", "Picklist")],
            lookups=[("programid", "Program", "program", True), ("regionid", "Region", "region", False),
                     ("categoryid", "Category", "category", False), ("customerid", "Customer", ["account", "contact"], True),
                     ("managerid", "Project Manager", "systemuser", False), ("fundingsourceid", "Funding Source", "fundingsource", False)])
    s.table("projectphase", "Project Phase", lookups=[("projectid", "Project", "project", True)])
    s.table("milestone", lookups=[("projectid", "Project", "project", True), ("phaseid", "Phase", "projectphase", False)])
    s.table("site", columns=[("address", "Address", "String")],
            lookups=[("regionid", "Region", "region", True), ("projectid", "Project", "project", False),
                     ("landownerid", "Landowner", "account", False)])
    s.table("structure", lookups=[("siteid", "Site", "site", True), ("projectid", "Project", "project", False),
                                  ("programid", "Program", "program", False), ("categoryid", "Category", "category", False)])
    s.table("structurecomponent", "Structure Component",
            lookups=[("structureid", "Structure", "structure", True), ("siteid", "Site", "site", False),
                     ("projectid", "Project", "project", False)])

    # Work
    s.table("task", lookups=[("projectid", "Project", "project", True), ("phaseid", "Phase", "projectphase", False),
                             ("milestoneid", "Milestone", "milestone", False), ("parenttaskid", "Parent Task", "task", False),
                             ("assignedtoid", "Assigned To", "systemuser", False)])
    s.table("taskdependency", "Task Dependency",
            lookups=[("predecessorid", "Predecessor", "task", True), ("successorid", "Successor", "task", True)])
    s.table("resource", lookups=[("userid", "User", "systemuser", False), ("vendorid", "Vendor", "account", False),
                                 ("regionid", "Region", "region", False)])
    s.table("skill", lookups=[("categoryid", "Category", "category", False)])
    s.table("assignment", lookups=[("resourceid", "Resource", "resource", True), ("taskid", "Task", "task", True),
                                   ("projectid", "Project", "project", False)])
    s.table("timesheet", lookups=[("resourceid", "Resource", "resource", True), ("approverid", "Approver", "systemuser", False)])
    s.table("timeentry", "Time Entry",
            lookups=[("timesheetid", "Timesheet", "timesheet", True), ("assignmentid", "Assignment", "assignment", False),
                     ("projectid", "Project", "project", False), ("taskid", "Task", "task", False)])

    # Finance
    s.table("budget", lookups=[("projectid", "Project", "project", True), ("fundingsourceid", "Funding Source", "fundingsource", False)])
    s.table("budgetline", "Budget Line", lookups=[("budgetid", "Budget", "budget", True), ("categoryid", "Category", "category", False)])
    s.table("contract", lookups=[("vendorid", "Vendor", "account", True), ("projectid", "Project", "project", False),
                                 ("programid", "Program", "program", False)])
    s.table("contractline", "Contract Line",
            lookups=[("contractid", "Contract", "contract", True), ("budgetlineid", "Budget Line", "budgetline", False)])
    s.table("purchaseorder", "Purchase Order",
            lookups=[("contractid", "Contract", "contract", True), ("projectid", "Project", "project", False),
                     ("vendorid", "Vendor", "account", False)])
    s.table("invoice", lookups=[("purchaseorderid", "Purchase Order", "purchaseorder", True), ("vendorid", "Vendor", "account", True),
                                ("projectid", "Project", "project", False)])
    s.table("invoiceline", "Invoice Line",
            lookups=[("invoiceid", "Invoice", "invoice", True), ("contractlineid", "Contract Line", "contractline", False),
                     ("budgetlineid", "Budget Line", "budgetline", False)])
    s.table("expense", lookups=[("resourceid", "Resource", "resource", True), ("projectid", "Project", "project", True),
                                ("budgetlineid", "Budget Line", "budgetline", False)])

    # Compliance
    s.table("permit", lookups=[("siteid", "Site", "site", True), ("projectid", "Project", "project", False),
                               ("agencyid", "Agency", "account", False)])
    s.table("permitcondition", "Permit Condition", lookups=[("permitid", "Permit", "permit", True)])
    s.table("inspection", lookups=[("siteid", "Site", "site", True), ("structureid", "Structure", "structure", False),
                                   ("projectid", "Project", "project", False), ("inspectorid", "Inspector", "resource", False),
                                   ("permitid", "Permit", "permit", False)])
    s.table("inspectionitem", "Inspection Item",
            lookups=[("inspectionid", "Inspection", "inspection", True), ("componentid", "Component", "structurecomponent", False)])
    s.table("finding", lookups=[("inspectionid", "Inspection", "inspection", True), ("itemid", "Item", "inspectionitem", False),
                                ("structureid", "Structure", "structure", False), ("siteid", "Site", "site", False)])
    s.table("correctiveaction", "Corrective Action",
            lookups=[("findingid", "Finding", "finding", True), ("ownerresourceid", "Owner", "resource", False),
                     ("taskid", "Task", "task", False)])

    # Governance
    s.table("risk", lookups=[("projectid", "Project", "project", True), ("categoryid", "Category", "category", False),
                             ("ownerid2", "Risk Owner", "contact", False)])
    s.table("issue", lookups=[("projectid", "Project", "project", True), ("riskid", "Risk", "risk", False),
                              ("raisedbyid", "Raised By", "contact", False)])
    s.table("changerequest", "Change Request",
            lookups=[("projectid", "Project", "project", True), ("issueid", "Issue", "issue", False),
                     ("contractid", "Contract", "contract", False), ("requestedbyid", "Requested By", "contact", False)])
    s.table("approval", lookups=[("changerequestid", "Change Request", "changerequest", False),
                                 ("invoiceid", "Invoice", "invoice", False), ("timesheetid", "Timesheet", "timesheet", False),
                                 ("approverid", "Approver", "systemuser", True)])
    s.table("document", lookups=[("projectid", "Project", "project", False), ("siteid", "Site", "site", False),
                                 ("contractid", "Contract", "contract", False), ("categoryid", "Category", "category", False)])
    s.table("documentreview", "Document Review",
            lookups=[("documentid", "Document", "document", True), ("reviewerid", "Reviewer", "contact", False)])
    s.table("meeting", lookups=[("projectid", "Project", "project", False), ("programid", "Program", "program", False)],
            activity=True)
    s.table("stakeholder", lookups=[("contactid", "Contact", "contact", True), ("projectid", "Project", "project", True),
                                    ("accountid", "Organisation", "account", False)])

    s.custom_lookup_on_oob("account", "regionid", "Region", "region")
    s.custom_lookup_on_oob("contact", "primaryprojectid", "Primary Project", "project")

    s.many_to_many("resource", "skill")
    s.many_to_many("document", "inspection")
    s.many_to_many("meeting", "contact")
    return s


def small():
    """Seven tables, one page: the golden-file fixture."""
    s = Solution("ContosoAssets", "Contoso Assets")
    s.oob("account", in_solution=True)
    s.table("site", lookups=[("ownerorgid", "Owner Organisation", "account", True)])
    s.table("asset", columns=[("serial", "Serial Number", "String")],
            lookups=[("siteid", "Site", "site", True), ("parentassetid", "Parent Asset", "asset", False)])
    s.table("workorder", "Work Order",
            lookups=[("assetid", "Asset", "asset", True), ("siteid", "Site", "site", False),
                     ("requestedbyid", "Requested By", ["account", "contact"], False),
                     ("technicianid", "Technician", "systemuser", False)])
    s.table("part")
    s.table("workorderpart", "Work Order Part",
            lookups=[("workorderid", "Work Order", "workorder", True), ("partid", "Part", "part", True)])
    s.many_to_many("asset", "part")
    return s


if __name__ == "__main__":
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "fixtures", "synthetic")
    os.makedirs(out, exist_ok=True)
    project_delivery().write(os.path.join(out, "project-delivery.json"))
    small().write(os.path.join(out, "assets.json"))
    print("wrote", os.path.normpath(out))
