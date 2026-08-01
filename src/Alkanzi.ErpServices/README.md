# Alkanzi.ErpServices

A self-contained, ERP-specific layer for the Fakhruddin ERP on Oracle 19c. It
names the actual tables, the actual document types, and the actual Oracle it
talks to.

**Standalone by design.** This project depends on nothing but EF Core and the
Oracle provider — no shared Alkanzi auditing library. Its approval engine,
audit-stamping interceptor, soft-delete filters and contracts are all its own,
so it can evolve without being pinned to another package.

Two things live here:

- **`ErpApprovalEngine`** — submit / approve / reject / rework any document
  type, dispatched to its transaction table for the current tenant. For
  workflow-bound documents it resolves the governing workflow, runs the level's
  `UPDATE_SENTENCE`, and appends to the approval log.
- **`ErpProcedureService`** — call Oracle stored procedures and functions over
  the same context connection.

It ships as a NuGet package, multi-targeting `net8.0;net10.0`. It maps a specific
ERP's tables, so treat it as an application-specific library rather than a
general-purpose one.

## Layout

```
ErpContracts.cs                 IErpAuditable / IErpApprovable / IErpWorkflowBound
                                IErpTransactionMenu / IErpUserProvider / IErpCompanyContext
                                ApprovalAction
ApprovalResult.cs               ApprovalResult + ApprovalOutcome (returned by the action methods)
ErpEntities.cs                  FM_TRANSACTION_MENU, FM_JOURNAL_HDR, CALL_REGISTERATION
WorkflowEntities.cs             SM_WORKFLOW_FORMS / _FORM_LEVELS, SM_APPROVAL_LOGS_HEADER / _DETAIL
                                TransactionWorkflow, ResolvedWorkflow
ErpDbContext.cs                 maps them, own soft-delete filters, attaches the interceptor
ErpAuditSaveChangesInterceptor.cs   stamps IErpAuditable, turns deletes into soft deletes
IErpApprovalEngine.cs           Get / Submit / Approve / Reject / Rework / ApplyApproval + workflow lookup
ErpApprovalEngine.cs              dispatch (docType -> table -> CLR type -> row) + transition + workflow + log
IErpProcedureService.cs         ExecuteAsync / QueryAsync / ExecuteScalarProcAsync
ErpProcedureService.cs            over ErpDbContext's connection
IErpApprovalProcessService.cs   RunAsync — SM_APPROVE_PROCESS / SM_REJECT_PROCESS wrapper
ErpApprovalProcessService.cs      transactional, commits on the procedure's success flag
IErpApprovalDashboardService.cs GetDataAsync — approval rows across a user's accessible doc types
ErpApprovalDashboardService.cs    resolves each doc type's table via FM_TRANSACTION_MENU;
                                  GetDepartmentEmployeesAsync — PANEL.DEPARTMENT_EMPLOYEES panel
ServiceCollectionExtensions.cs  AddErpApprovalEngine / AddErpProcedureService / AddErpApprovalProcessService
                                AddErpApprovalDashboardService / AddErpDbContext
```

## Wiring it up

The approval engine saves through the context, so the audit interceptor must be
attached to `ErpDbContext` for approvals to stamp `UPDATED_BY` / `UPDATED_AT`.

```csharp
// Your own acting-user implementation. Tenant (ORG/COMP/BRANCH) is read from the
// transaction row, so no company context is registered.
services.AddErpApprovalEngine<CurrentUser>();
services.AddErpProcedureService();
services.AddErpApprovalProcessService();   // SM_APPROVE_PROCESS / SM_REJECT_PROCESS wrapper

// The ERP has its own connection — independent of any other DbContext or
// connection the host API uses. Give it its own (e.g. named) connection string.
services.AddErpDbContext(config.GetConnectionString("Erp")!);
```

`ErpDbContext` is standalone: it shares nothing with the host's other contexts,
so its connection string is entirely separate. Everything an approval touches —
the status change, the level's `UPDATE_SENTENCE`, and the approval-log writes —
flows through this one context's single connection, so it commits or rolls back
as one unit of work.

`AddErpDbContext` pins Oracle 19c compatibility and attaches the audit
interceptor **exactly once** (when `AddErpApprovalEngine` has registered one).
Attach it only here *or* through the context constructor — never both, or every
row stamps twice. If you would rather wire the context by hand:

```csharp
services.AddDbContext<ErpDbContext>((sp, options) => options
    .UseOracle(connectionString, o => o.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19))
    .AddInterceptors(sp.GetRequiredService<ErpAuditSaveChangesInterceptor>()));
```

Where `CurrentUser : IErpUserProvider` returns the acting user id. Tenant columns
come from the transaction row (`IErpTenantScoped`), so there is no company context
to implement.

### Mapping your own approvable tables

The engine dispatches `docType → FM_TRANSACTION_MENU.TABLE_NAME →` the CLR type
mapped to that table in **the engine's context model**. Out of the box only
`CALL_REGISTERATION` and `FM_JOURNAL_HDR` are mapped, so to approve your own
tables you map them on a **subclass of `ErpDbContext`** and register it with the
typed `AddErpDbContext<TContext>` overload — the engine, dashboard and procedure
services all resolve `ErpDbContext`, so they get your subclass and everything it
maps:

```csharp
public sealed class FlexionErpDbContext : ErpDbContext
{
    // Base ctor takes the non-generic DbContextOptions; the subclass takes its own.
    public FlexionErpDbContext(DbContextOptions<FlexionErpDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // keep the registry, log and workflow tables
        b.Entity<PurchaseOrderHeader>(e =>   // PurchaseOrderHeader : IErpApprovable, IErpAuditable, IErpTenantScoped
        {
            e.HasKey(x => x.ID);
            e.ToTable("PO_HDR");
            e.Property(x => x.ID).ValueGeneratedNever();
            e.HasQueryFilter(x => x.IS_DELETED != true);
        });
        // ... one block per approvable table
    }
}

services.AddErpApprovalEngine<CurrentUser>();
services.AddErpDbContext<FlexionErpDbContext>(config.GetConnectionString("Erp")!);
```

Each entity must implement `IErpApprovable` (plus `IErpAuditable`,
`IErpTenantScoped`, and `IErpWorkflowBound` if workflow-bound). You can reuse the
**same entity classes** your host context already defines — a CLR type may be
mapped in more than one `DbContext`. This is a **separate** context from your
host's (e.g. an `IdentityDbContext`), which cannot itself derive from
`ErpDbContext`; both simply map the shared entity types over the same connection.

> **Oracle 19c:** pin `UseOracleSQLCompatibility(DatabaseVersion19)`. The 23.x
> provider defaults to 23ai SQL and emits the native `BOOLEAN` type, which 19c
> rejects with ORA-00902.

## Approvals

The action methods (`Submit`/`Approve`/`Reject`/`Rework`/`ApplyApproval`) return
an `ApprovalResult`, not the row — so ordinary "you can't do that" answers are
values to render, not exceptions to catch:

```csharp
public sealed class JournalController(IErpApprovalEngine approvals)
{
    public async Task<IActionResult> Submit(int id)
    {
        var result = await approvals.SubmitAsync("JournalVoucher", id);

        return result.Outcome switch
        {
            ApprovalOutcome.Applied         => Ok(result.Row),
            ApprovalOutcome.NotFound        => NotFound(result.Message),
            ApprovalOutcome.AlreadyApproved => Conflict(result.Message),      // 409
            ApprovalOutcome.AlreadyRejected => Conflict(result.Message),      // 409
            ApprovalOutcome.NotAuthorized   => Forbid(),                      // 403
            ApprovalOutcome.NoWorkflow      => UnprocessableEntity(result.Message),
            ApprovalOutcome.NotApprovable   => UnprocessableEntity(result.Message),
            ApprovalOutcome.ProcessFailed   => UnprocessableEntity(result.Message),
            _                               => BadRequest(result.Message),
        };
    }
}
```

`ApprovalResult` carries `Outcome` (branch on this), `Status` (`true` only when
applied), `Message` (for display/logging), and `Row` (the affected transaction —
also handed back on the `Already*` outcomes so you can show current state).

Each call resolves the document type through `FM_TRANSACTION_MENU`, resolves its
`TABLE_NAME` to a mapped CLR type through EF's model, loads the row, computes the
transition, and — inside one transaction — applies the row through EF, runs the
level's `UPDATE_SENTENCE`, drives the ERP approval procedure, and writes the log
(see *Running the ERP approval procedures*):

| Action  | `APPROVE_STATUS` | `APPROVE_LEVEL`     |
|---------|------------------|---------------------|
| Submit  | 1                | +1                  |
| Rework  | 2                | = `targetLevel`     |
| Reject  | 3                | unchanged           |
| Approve | 4                | +1                  |

A row that is already **approved** or **rejected** is terminal: only `Rework`
applies to it (the correction path that reopens it), and any other action comes
back as `AlreadyApproved` / `AlreadyRejected`. Landing a climbing action on the
workflow's final level forces `Approve` and stamps `DIGIT_SIGNATURE` — an AES
signature of the transaction id, keyed to match the ERP's own scheme.

**Results vs exceptions.** Outcomes that are normal answers to a user action come
back as an `ApprovalResult`: `NotFound` (no row / soft-deleted), `NotApprovable`
(the table has no approval columns), `AlreadyApproved`, `AlreadyRejected`,
`NotAuthorized` (the acting user may not act at this level — see *Authorization*),
`NoWorkflow` (a workflow-bound row with no workflow configured), and `ProcessFailed`
(the ERP approval procedure reported failure — the whole action rolled back).
Genuine misuse and misconfiguration still throw — an undefined `ApprovalAction` or
a `targetLevel` on a non-`Rework` action (`ArgumentException` /
`ArgumentOutOfRangeException`), and a document type that is unconfigured or resolves
to a table not mapped on `ErpDbContext` (`InvalidOperationException`).

Every action method also takes an optional `remarks` (recorded on the log detail
row) and an `sgId` (security group, for authorization):

```csharp
await approvals.SubmitAsync("callRegistration", id, remarks: "Looks good", sgId: securityGroupId);
await approvals.RejectAsync("callRegistration", id, remarks: "Missing invoice", sgId: securityGroupId);
await approvals.ApplyApprovalAsync("callRegistration", id, ApprovalAction.Approve, sgId: securityGroupId);
```

Dispatch is dynamic: a new transaction table needs only to be mapped on
`ErpDbContext`, not named in the engine.

## Workflows

Some document types are **workflow-bound** (they carry a `WORKFLOW_ID`, e.g.
`CALL_REGISTERATION`); others are approvable but not (`FM_JOURNAL_HDR`). On every
applied transition the engine resolves the governing workflow — a no-op for the
non-bound ones — through two steps:

1. **`GetWorkflowsAsync`** calls `APPROVAL_REVERT_PAK.GET_TRANS_WF` and returns
   one `TransactionWorkflow` per configured `WF_ID`, each carrying the `MAP_FUN`
   that disambiguates when a document type has several.
2. **`ResolveWorkflowAsync`** picks the single workflow for a transaction — the
   sole one, or the one `MAP_FUN(transId, docType)` selects — and loads its
   `SM_WORKFLOW_FORMS` form and ordered `SM_WORKFLOW_FORM_LEVELS`. It returns a
   `ResolvedWorkflow` (id, final level, form, levels), or `null` when nothing is
   configured.

Both are on `IErpApprovalEngine`, so you can call them directly to inspect a
document's workflow without applying anything.

**Binding and re-use.** A row *entering* the chain — still at level 0, or not yet
bound (`WORKFLOW_ID` null or 0) — is resolved by document type (the two steps
above), and the resolved id is **stamped onto `WORKFLOW_ID`**. A row already bound
skips the `GET_TRANS_WF`/`MAP_FUN` round-trips and loads its workflow **directly by
`WORKFLOW_ID`**, so every later action stays on the same workflow.

**Final level.** When a *climbing* action (Submit/Approve) lands the row on the
form's `LAST_LEVEL`, the status is forced to `Approve` — the chain is complete. A
Reject or Rework that happens to sit on the final level keeps its own status.

**Authorization.** Every action on a workflow-bound row is gated on
`APPROVAL_REVERT_PAK.LVL_AUTHORIZATION(wf, level, sg, usr, overlap, doc, transId)`
— an Oracle function returning `'flag,message'` (`1` authorized, `0` not). The
security group is the `sgId` argument (on every action method, including the
verbs); the acting `usr` comes from the injected `IErpUserProvider`. A denial
comes back as an `ApprovalOutcome.NotAuthorized` result (with the function's
message), not an exception. Rows with no workflow (e.g. a journal voucher) are
not gated.

**`UPDATE_SENTENCE`.** Each level may carry an `UPDATE_SENTENCE` — a SET-clause
fragment the ERP attaches to reaching that level. When the row's new
`APPROVE_LEVEL` matches a level that has one, the engine runs
`UPDATE <table> SET <UPDATE_SENTENCE> WHERE ID = <transId>` on the same context
connection, so it commits or rolls back with the status change. The table name
and fragment come from ERP configuration (not the caller), so they are
interpolated; `transId` is bound as a parameter.

## The approval log

Every applied transition is recorded in three tables:

- **`SM_APPROVAL_LOGS_HEADER`** — one row per `(DOC_NAME, TRANSACTION_ID)`,
  created the first time a transaction is logged and flipped to `IS_APPROVED`
  once the row reaches full approval. Carries `DOC_ID` / `FORM_ID` from the
  resolved workflow (0 when the document is not workflow-bound).
- **`SM_APPROVAL_LOGS_DETAIL`** — one row per action beneath the header, holding
  the level it was taken at (`FROM_LEVEL`), the level's name
  (`FROM_LEVEL_NAME`, from `SM_WORKFLOW_FORM_LEVELS.REMARKS`), the status it
  moved to (`APPROVE_STATUS`), and the caller's `REMARKS` for the action.
- **`SM_TRANS_HISTORY`** — the ERP's history trail, one row per action:
  `ACTION` = the new status, `TRANS_STATUS` = the level, `STATUS_NAME` = the level
  name, plus `POSTED_BY` / `POST_DATE`. A plain table (not `IErpAuditable`).

Timestamps are **server-local** (`DateTime.Now`) throughout — the audit interceptor
and `SM_TRANS_HISTORY.POST_DATE` — matching the ERP's own convention.

**Tenant comes from the row.** When the transaction row implements
`IErpTenantScoped` (exposes `ORG_ID` / `COMP_ID` / `BRANCH_ID`), the log takes those
from the row itself — not from an ambient context. A row without the columns logs
`0`.

The writes go through the same context and enlist in the same transaction as the
status change, so a rolled-back approval leaves no log behind. Audit columns are
stamped by the interceptor, as everywhere else — `IErpAuditable` also exposes
`MarkCreated` / `MarkUpdated` / `MarkDeleted` for code paths that don't run through
it. The log tables' `ID` is **store-generated** (the header's key is read back to
link the detail), which assumes they are Oracle identity columns — switch the
mapping in `ErpDbContext` to a sequence if yours assign `ID` by trigger instead.

> **Not yet populated:** `IP` and `HOST_NAME` on the detail row — there's no
> request-context argument yet; thread one through the action methods to fill them.

## Notifications

The engine sends nothing itself — a reusable data library shouldn't own SMTP.
Instead, an `Applied` result carries an **`ApprovalNotification`** with everything a
notification needs (docType, transId, workflow id, acting user, from/to level,
status, main doc type, branch, display name, initiator). The host reads it and
calls its own email service (which typically enqueues to a background worker, so
there's no delay):

```csharp
var result = await approvals.ApproveAsync(docType, id, sgId: sgId);

if (result.Status && result.Notification is { } n)
{
    await emailService.Post("APPROVAL", n.WorkflowId, n.DocType, n.TransId, n.ActingUser,
        n.FromLevel, n.ToLevel, n.Status, n.MainDocType, n.BranchId, n.DisplayName, n.Initiator);
}
```

`Notification` is populated only for `Applied`; it's `null` on every other outcome.

## Running the ERP approval procedures

The transition is driven by the ERP's own PL/SQL — the workflow-stack routing
lives there. On every applied action `ApplyApprovalAsync` calls, through
`IErpApprovalProcessService`, one of:

- **`SM_APPROVE_PROCESS`** for most actions,
- **`SM_REJECT_PROCESS`** for `Reject`.

The engine applies the row itself through EF, so it passes a **null `STR_QUERY`**
— the procedure drives the workflow stack rather than the UPDATE. It supplies the
doc/tenant/user context (`mainDocType` = `FM_TRANSACTION_MENU.MAIN_DOC_TYPE`,
`docDate` = the row's `DOC_DATE` formatted `DD-MON-YY`), reads the OUT `MSG` of the form
`'message,flag'` (`1` success, `0` failure), and on a `0` flag rolls the whole
action back and returns `ApprovalOutcome.ProcessFailed`.

The EF row update, the `UPDATE_SENTENCE`, the procedure call, and the log write
all run in **one transaction** the engine owns — or enlists in the caller's, if
one is already open — so any failure undoes everything together.

You can also call the service directly with a query you build yourself:

```csharp
var result = await approvalProcess.RunAsync(
    ApprovalAction.Submit,
    query: "UPDATE ... WHERE ID = ...",   // or null to let the caller apply the row
    mainDocType: mainDocType, docType: docType, transId: id,
    approveStatus: status, userId: userId,
    orgId: org, compId: comp, branchId: branch, docDate: docDate);

if (!result.Success) return BadRequest(result.Message);
```

> If a procedure issues its own `COMMIT` internally, that commit ends the
> transaction — the rollback can only undo work still pending. Verify your
> procedures leave the commit to the caller.

## Approval dashboard

`IErpApprovalDashboardService` reads approval rows across the document types a user
has access to — for each, it resolves `TABLE_NAME` from `FM_TRANSACTION_MENU`,
loads the mapped approvable table, and enriches the rows with the menu's
`DISPLAY_NAME` / `MAIN_DOC_TYPE`:

```csharp
services.AddErpApprovalDashboardService();

// The host supplies the doc types the user may see (its own permission model).
var rows = await dashboard.GetDataAsync(userDocTypes, ApprovalDashboardFilter.Pending);
// rows: Id, DocType, DocDate, ApproveStatus, ApproveLevel, WorkflowId,
//       CreatedBy, CreatedAt, DisplayName, MainDocType
```

`ApprovalDashboardFilter` selects by status: **`All`**, **`Pending`** (not yet
terminal — status 0/1/2), **`Approved`** (4), **`Rejected`** (3).

It's **permission-scoped by doc type** (the caller passes the accessible menus), one
query for the menus plus one per table (no N+1), and **generic** — a new approvable
table just needs mapping on `ErpDbContext`, no dashboard change. `Id` / `CREATED_BY`
/ `CREATED_AT` are read via `EF.Property`; a table shared by several doc types is
filtered by `DOC_TYPE` so each menu sees only its own rows.

### Department-employee panel

The same `IErpApprovalDashboardService` also exposes the department-employee
panel: `GetDepartmentEmployeesAsync(departmentId)` runs
`PANEL.DEPARTMENT_EMPLOYEES(P_DEPARTMENT_ID)` and maps its `OUT_CURSOR` to
`DepartmentEmployee` rows.

```csharp
var employees = await dashboard.GetDepartmentEmployeesAsync(departmentId);
// employees: Id, UserId, Employee, Profile, DepartmentName, DepartmentId,
//            DesignationId, Designation, Status, IsOnline

var online = employees.Where(e => e.IsOnline).ToList();
```

Columns are mapped **by name** (order-independent) and **tolerant of absence**, so
a query that omits a column just yields `null`/`0` rather than throwing. `IsOnline`
derives from `STATUS` — `Present` / `Online` count as online (case-insensitive,
trimmed); `Absent`, `On Annual leave`, … are offline. Extend the online set in
`DepartmentEmployee`, or the routine / cursor / parameter names in
`ErpApprovalDashboardService`, if the procedure changes.

## Calling Oracle procedures and functions

`ErpProcedureService` gives you one primitive plus two typed conveniences.

**`ExecuteAsync`** — the primitive. Manages the connection, command type and
ambient transaction, then hands you the live command. Provider-agnostic
(`DbCommand`); cast to `OracleCommand` for Oracle specifics.

**`QueryAsync<T>`** — a procedure whose OUT REF CURSOR is read into a `List<T>`:

```csharp
var rows = await procedures.QueryAsync(
    "FM_PKG.GET_PENDING_APPROVALS",
    cursorParameter: "p_result",
    map: r => new PendingApproval(r.GetInt32(0), r.GetString(1)),
    parameters: new Dictionary<string, object?> { ["p_comp_id"] = 6 });
```

**`ExecuteScalarProcAsync<T>`** — a procedure with one OUT scalar
(`string`/`int`/`long`/`decimal`/`double`/`DateTime`):

```csharp
var docNum = await procedures.ExecuteScalarProcAsync<int>(
    "FM_PKG.NEXT_DOC_NUMBER",
    outParameter: "p_number",
    parameters: new Dictionary<string, object?> { ["p_doc_type"] = "JournalVoucher" });
```

**A function, or anything the conveniences don't cover** — drop to `ExecuteAsync`:

```csharp
var balance = await procedures.ExecuteAsync(
    "FM_PKG.ACCOUNT_BALANCE",
    async command =>
    {
        var oracle = (OracleCommand)command;
        oracle.BindByName = true;
        var ret = new OracleParameter("ret", OracleDbType.Decimal, ParameterDirection.ReturnValue);
        oracle.Parameters.Add(ret);   // return value binds first for a function
        oracle.Parameters.Add(new OracleParameter("p_account_id", OracleDbType.Int32) { Value = 1001 });

        await oracle.ExecuteNonQueryAsync();
        return ((OracleDecimal)ret.Value).Value;
    });
```

The conveniences set `BindByName = true` for you; when you drop to `ExecuteAsync`,
set it yourself for any routine with more than one parameter — Oracle binds by
position otherwise. If the context has an open transaction, every call enlists in
it, so a procedure and a `SaveChanges` commit or roll back together.

## Tests

`Alkanzi.ErpServices.OracleTests` runs against a live ERP connection, supplied
through user secrets or the `ALKANZI_ORACLE_CONNECTION` environment variable
(same key as the other Oracle tests, so one setting serves them all). Without a
connection the tests **skip** rather than fail. The test classes share one xUnit
collection so they run sequentially — concurrent Oracle connections make REF
CURSOR reads flaky.

- Approval tests dispatch and transition real rows, each inside a transaction
  that is rolled back — nothing persists to the ERP.
- Workflow tests exercise the `GET_TRANS_WF` procedure, the `MAP_FUN`
  disambiguation, and the `SM_WORKFLOW_FORMS` / `_LEVELS` load — read-only.
- Procedure tests use anonymous PL/SQL blocks over `dual`, so their assertions
  are deterministic and depend on no schema object, while the binding and
  cursor-reading code is exactly what a real procedure call uses.

```bash
dotnet user-secrets --project tests/Alkanzi.ErpServices.OracleTests \
  set "Oracle:ConnectionString" "User Id=...;Password=...;Data Source=..."

dotnet test tests/Alkanzi.ErpServices.OracleTests
```
