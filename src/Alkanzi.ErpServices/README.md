# Alkanzi.ErpServices

A self-contained, ERP-specific layer for the Fakhruddin ERP on Oracle 19c. It
names the actual tables, the actual document types, and the actual Oracle it
talks to.

**Standalone by design.** This project depends on nothing but EF Core and the
Oracle provider — no shared Alkanzi auditing library. Its approval engine,
audit-stamping interceptor, soft-delete filters and contracts are all its own,
so it can evolve without being pinned to another package.

Three things live here:

- **`ErpApprovalEngine`** — submit / approve / reject / rework any document
  type, dispatched to its transaction table for the current tenant. For
  workflow-bound documents it resolves the governing workflow, runs the level's
  `UPDATE_SENTENCE`, and appends to the approval log.
- **`ErpApprovalDashboardService`** — read side of the same model: approval rows
  across a set of document types, or the transactions actually waiting on one
  user id.
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
                                  GetUserScopeAsync / GetUserDataAsync — what a user id may
                                  approve, and the rows actually waiting on them;
                                  GetDepartmentEmployeesAsync — PANEL.DEPARTMENT_EMPLOYEES panel
ServiceCollectionExtensions.cs  AddErpApprovalEngine / AddErpProcedureService / AddErpApprovalProcessService
                                AddErpApprovalDashboardService / AddErpDbContext
```

## Wiring it up

The engine reads and writes **everything by table name with raw SQL** — the
transaction row and the approval-infrastructure tables (`FM_TRANSACTION_MENU`, the
workflow, log and history tables). It needs **no entity types** and does **not**
touch your application's `DbContext`; it runs on its own `ErpDbContext`, which is
just a connection over the ERP connection string.

```csharp
// Your own acting-user implementation. Tenant (ORG/COMP/BRANCH), the initiator and
// the doc date are all read from the transaction row.
services.AddErpApprovalEngine<CurrentUser>();
services.AddErpApprovalDashboardService();

// The engine's own connection — a (e.g. named) ERP connection string, independent
// of any other DbContext the host uses.
services.AddErpDbContext(config.GetConnectionString("Erp")!);
```

That's the whole integration. To approve a new table, just add its
`FM_TRANSACTION_MENU` row (`DOC_TYPE` → `TABLE_NAME`) — the engine `UPDATE`s that
table by name and writes the log; nothing in code changes. The table only needs the
standard columns every ERP transaction carries (`APPROVE_STATUS`, `APPROVE_LEVEL`,
`WORKFLOW_ID`, `DIGIT_SIGNATURE`, `DOC_DATE`, `REMARKS`, `DOC_TYPE`, the tenant and
audit columns). Everything an approval touches — the row's status change, the
level's `UPDATE_SENTENCE`, the approval-procedure call and the log writes — runs on
this one connection in a single transaction, so it commits or rolls back as one.

Where `CurrentUser : IErpUserProvider` returns the acting user id.

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

### What happens when it fails

The row update, the `UPDATE_SENTENCE`, the procedure call and the log write all run
in **one transaction** — the engine's own, or the caller's if one is already open.
Failures arrive in two shapes:

- **The ERP refuses the transition.** `IErpApprovalProcessService` converts every
  Oracle error into a failed result, so this **never throws**. You get
  `Status == false`, `Outcome == ApprovalOutcome.ProcessFailed`, and the ERP's own
  text in `Message` (e.g. `ORA-20111: ...`).
- **Something genuinely breaks** — a bad `UPDATE_SENTENCE`, the log write, the
  commit. The exception propagates to you.

Ordinary refusals — `NotFound`, `NotApprovable`, `AlreadyApproved`,
`AlreadyRejected`, `NotAuthorized`, `NoWorkflow` — are results, not exceptions, and
return before anything is written.

So a host must check `Status` **and** catch:

```csharp
try
{
    var result = await approvals.SubmitAsync(docType, id, sgId: sgId);
    if (!result.Status) return BadRequest($"{result.Outcome}: {result.Message}");
}
catch (Exception ex) { /* log; the engine has already undone its own work */ }
```

**Who rolls back.** When the engine opens the transaction it rolls the whole thing
back on either kind of failure. When **you** opened it, the engine must not roll it
back — that would discard your work — so it takes a **savepoint** before the row
update and rolls back to it instead. Without that, a refused approval would leave
the mutated `APPROVE_STATUS` / `APPROVE_LEVEL` pending in your transaction, and your
next commit would persist a failed approval. On a provider without savepoint support
this degrades to the old behaviour, where undoing is the caller's job.

The simplest option remains: **don't open a transaction around the engine** unless
you have other work to bundle atomically, and let it manage both paths itself.

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
//       CreatedBy, CreatedAt, DisplayName, MainDocType, BranchId, CompId
```

`ApprovalDashboardFilter` selects by status: **`All`**, **`Pending`** (not yet
terminal — status 0/1/2), **`Approved`** (4), **`Rejected`** (3).

It's **permission-scoped by doc type** (the caller passes the accessible menus), one
query for the menus plus one per table (no N+1), and **generic** — a new approvable
table just needs mapping on `ErpDbContext`, no dashboard change. Every column is read
by raw SQL on the resolved table name, so no entity type is required; a table shared
by several doc types is filtered by `DOC_TYPE` so each menu sees only its own rows.

### What is waiting on one user

`GetDataAsync` answers "rows for these doc types". It does **not** answer "rows this
user can act on" — for that, pass a user id:

```csharp
// The transactions actually pending THIS user's approval.
var mine = await dashboard.GetUserDataAsync(userId);           // Pending by default

// Optional: the (workflow form, level) pairs behind that list — useful for menus,
// or for explaining why something is or isn't on it.
var scope = await dashboard.GetUserScopeAsync(userId);
// scope: FormId, LevelId, WorkflowName, LastLevel, TableName,
//        DocType, DisplayName, MainDocType

// The user's security groups (id + name), collapsed to one row per group.
var groups = await dashboard.GetUserSecurityGroupsAsync(userId);
// groups: SecurityGroupId, Name
```

A user reaches a level through `SM_DIVISION_SECURITY_GROUPS_USERS` →
`SM_WORKFLOW_LVL_SECURITY_GROUPS` → `SM_WORKFLOW_FORMS`, and rows are matched on
**(`WORKFLOW_ID`, `APPROVE_LEVEL`)** — the form and the level together — where the
level is **one below** the user's authorised level. A transaction awaiting the
level-`L` approver currently sits at `APPROVE_LEVEL = L - 1`, so `GetUserScopeAsync`
returns `LevelId = L` while `GetUserDataAsync` matches `APPROVE_LEVEL = L - 1`.
`Pending` additionally narrows to **`APPROVE_STATUS IN (1, 2)`** (submitted /
reworked) — the rows actively awaiting a decision.

Matching on document type alone is wrong twice over:

- **One table serves many doc types.** `PRF_TRANSACTIONS` backs ~25 of them,
  `FM_RECEIPTS_MASTER` and `FM_FUND_MASTER` around 10 each.
- **One doc type runs under several workflow forms, at different levels.**
  `serviceLPO` alone spans five forms sitting at levels 3, 4, 4, 5 and 5.

So a doc-type filter hands back transactions the user has no authority over.
`APPROVE_LEVEL` is the right half of the pair because it is exactly what the engine
authorises against — `ApplyApprovalAsync` passes the row's current `APPROVE_LEVEL` to
`APPROVAL_REVERT_PAK.LVL_AUTHORIZATION`, so the dashboard list and the approve button
agree.

Details worth knowing:

- The same (form, level) commonly arrives through **several security groups**; they
  are collapsed, so a transaction appears once.
- Queries are grouped **one per table**, not one per doc type, using Oracle's
  multi-column `IN ((:w0, :l0), …)`, chunked at 250 pairs. For a user with ~130
  (form, level) pairs that is ~29 round trips rather than ~130.
- Rows with a **null `WORKFLOW_ID`** are never returned — with no workflow there is no
  level to authorise against.
- A `TABLE_NAME` that is not a plain identifier is **skipped**, not interpolated into
  SQL.
- Pass a `filter` to widen beyond `Pending` — the same
  `ApprovalDashboardFilter` values apply.

> Take care that the user id reaching `GetUserDataAsync` is a real one. `USER_ID` is
> just a number to this query, and a "missing user" sentinel can be a live row: in the
> Fakhruddin ERP, `USER_ID = -1` genuinely exists in
> `SM_DIVISION_SECURITY_GROUPS_USERS` and grants 66 (form, level) pairs.

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

## Version history

### 4.0.5

- **`GetUserDataAsync` now lists what is *pending for this user to approve*, matched one level below the user's authorised level.** Previously it matched transactions whose `APPROVE_LEVEL` equalled the user's scope `LEVEL_ID`. It now matches `APPROVE_LEVEL = LEVEL_ID - 1` — a transaction awaiting the level-L approver currently sits at `APPROVE_LEVEL = L - 1` — **and** narrows the Pending filter to `APPROVE_STATUS IN (1, 2)` (submitted / reworked, i.e. actively awaiting a decision) instead of `NOT IN (3, 4)` (which also let drafts/suspended through). Equivalent to:
  ```sql
  SELECT * FROM <table>
   WHERE (WORKFLOW_ID, APPROVE_LEVEL) IN ((:form, :userLevel - 1), …)
     AND APPROVE_STATUS IN (1, 2)
  ```
  Only the `GetUserDataAsync` path (`QueryUserTableAsync`) changed its level/status logic; `GetUserScopeAsync` is unchanged (the scope still returns the real `LEVEL_ID`).

- **New `GetUserSecurityGroupsAsync(userId)`** on `IErpApprovalDashboardService` — returns the user's security groups as `UserSecurityGroup(SecurityGroupId, Name)` from `SM_DIVISION_SECURITY_GROUPS_USERS` joined to `SM_SECURITY_GROUPS_MASTER` (grouped, so a group reached through several division rows appears once).

- **`ApprovalDashboardRow` now carries `BranchId` and `CompId` (both `int?`).** Both `GetDataAsync` and `GetUserDataAsync` select `BRANCH_ID` and `COMP_ID` from the transaction table and map them. They're optional trailing properties, so existing positional construction is unaffected. A table without either column is skipped by the same `OracleException` guard that already skips tables missing the other approval columns.

### 4.0.4

- **Pinned the Oracle provider back to `8.23.60` (net8) to restore the `23.6.0`
  managed driver.** `8.23.90` pulls `Oracle.ManagedDataAccess.Core` in the
  `[23.9.0, 24.0.0)` range; the **23ai (23.9.0) managed driver attempts a
  container-scoped step during connection-pool establishment that an Oracle 19c
  NON-CDB rejects** with `ORA-65090: operation only allowed in a container
  database`. The app sees it as `ORA-50092: The requested connection could not be
  established`, **intermittently and app-wide** (only when the pool opens a *new*
  physical connection — so single-connection tools connect fine while a busy app
  fails). `23.6.0` does not do this. No code or API change: the Oracle ADO.NET
  types the procedure wrappers use compile unchanged against both provider majors.
  The `net10` target (`10.23.26200`) is unchanged.

  Symptom: sporadic `ORA-50092` / `ORA-65090` on connection open across unrelated
  endpoints and background jobs on a 19c non-CDB, after upgrading past `8.23.60`.

### 4.0.3

- **Fixed: an approval could advance the level without applying `DOC_STATUS`.**
  The workflow levels were loaded without filtering `IS_DELETED`. Reconfiguring a
  level soft-deletes the old row and inserts a new one, so a form can hold two rows
  for the same `LEVEL_ID` — the live one carrying `UPDATE_SENTENCE`, the retired one
  carrying `NULL`. The level lookup could pick the retired row, and the transition
  would then move `APPROVE_LEVEL` (a bound parameter) while silently skipping
  `DOC_STATUS` (part of the sentence). Levels are now filtered, and ordered so that a
  form with two live rows for one level deterministically prefers the one that
  carries a sentence. `SM_WORKFLOW_FORMS` is likewise filtered.

  Symptom: a document climbs the chain but its `DOC_STATUS` stays where it was.
  Rows that transitioned before the fix keep the stale status — each level only sets
  its own status on transition, so a later approval will not repair an earlier miss.

- **Fixed: a refused approval could leave its row update pending in a caller-owned
  transaction.** The engine cannot roll back a transaction it does not own, so a
  `ProcessFailed` used to return with the mutated `APPROVE_STATUS` / `APPROVE_LEVEL`
  still pending — ready to be committed by whatever the caller did next. It now takes
  a savepoint before the row update and rolls back to it. See
  [What happens when it fails](#what-happens-when-it-fails).

### 4.0.2

Added `GetUserScopeAsync` / `GetUserDataAsync` to the approval dashboard —
transactions waiting on a specific user id, matched on
(`WORKFLOW_ID`, `APPROVE_LEVEL`).

> Carries both bugs listed under 4.0.3. Prefer 4.0.3.
