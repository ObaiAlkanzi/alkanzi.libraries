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
ServiceCollectionExtensions.cs  AddErpApprovalEngine / AddErpProcedureService / AddErpApprovalProcessService / AddErpDbContext
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
            _                               => BadRequest(result.Message),
        };
    }
}
```

`ApprovalResult` carries `Outcome` (branch on this), `Status` (`true` only when
applied), `Message` (for display/logging), and `Row` (the affected transaction —
also handed back on the `Already*` outcomes so you can show current state).

Each call resolves the document type through `FM_TRANSACTION_MENU` (scoped to the
current tenant), resolves its `TABLE_NAME` to a mapped CLR type through EF's
model, loads the row, applies the transition, and saves:

| Action  | `APPROVE_STATUS` | `APPROVE_LEVEL`     |
|---------|------------------|---------------------|
| Submit  | 1                | +1                  |
| Rework  | 2                | = `targetLevel`     |
| Reject  | 3                | unchanged           |
| Approve | 4                | +1                  |

A row that is already **approved** or **rejected** is terminal: only `Rework`
applies to it (the correction path that reopens it), and any other action comes
back as `AlreadyApproved` / `AlreadyRejected`.

**Results vs exceptions.** Outcomes that are normal answers to a user action come
back as an `ApprovalResult`: `NotFound` (no row / soft-deleted), `NotApprovable`
(the table has no approval columns), `AlreadyApproved`, `AlreadyRejected`,
`NotAuthorized` (the acting user may not act at this level — see *Authorization*),
and `NoWorkflow` (a workflow-bound row with no workflow configured). Genuine misuse
and misconfiguration still throw — an undefined `ApprovalAction` or a `targetLevel`
on a non-`Rework` action (`ArgumentException` / `ArgumentOutOfRangeException`), and
a document type that is unconfigured or resolves to a table not mapped on
`ErpDbContext` (`InvalidOperationException`).

Every action method also takes an optional `remarks` (recorded on the log detail
row) and `ApplyApprovalAsync` an optional `sgId` (security group, opt-in
authorization):

```csharp
await approvals.SubmitAsync("callRegistration", id, remarks: "Looks good");
await approvals.RejectAsync("callRegistration", id, remarks: "Missing invoice");
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

Every applied transition is recorded in two tables:

- **`SM_APPROVAL_LOGS_HEADER`** — one row per `(DOC_NAME, TRANSACTION_ID)`,
  created the first time a transaction is logged and flipped to `IS_APPROVED`
  once the row reaches full approval. Carries `DOC_ID` / `FORM_ID` from the
  resolved workflow (0 when the document is not workflow-bound).
- **`SM_APPROVAL_LOGS_DETAIL`** — one row per action beneath the header, holding
  the level it was taken at (`FROM_LEVEL`), the level's name
  (`FROM_LEVEL_NAME`, from `SM_WORKFLOW_FORM_LEVELS.REMARKS`), the status it
  moved to (`APPROVE_STATUS`), and the caller's `REMARKS` for the action.

**Tenant comes from the row.** When the transaction row implements
`IErpTenantScoped` (exposes `ORG_ID` / `COMP_ID` / `BRANCH_ID`), the log takes those
from the row itself — not from an ambient context. A row without the columns logs
`0`.

The writes go through the same context and enlist in the same transaction as the
status change, so a rolled-back approval leaves no log behind. Audit columns are
stamped by the interceptor, as everywhere else. The log tables' `ID` is
**store-generated** (the header's key is read back to link the detail), which
assumes they are Oracle identity columns — switch the mapping in `ErpDbContext`
to a sequence if yours assign `ID` by trigger instead.

> **Not yet populated:** `IP` and `HOST_NAME` on the detail row — there's no
> request-context argument yet; thread one through the action methods to fill them.

## Running the ERP approval procedures

For document types whose approval is driven by the ERP's own PL/SQL — the
workflow-stack routing lives there, not in this engine — `IErpApprovalProcessService`
wraps the two procedures:

- **`SM_APPROVE_PROCESS`** for most actions,
- **`SM_REJECT_PROCESS`** for `Reject`.

Both take the `STR_QUERY` UPDATE the procedure executes, the doc/tenant/user
context, and return an OUT `MSG` of the form `'message,flag'` (`1` success, `0`
failure). `RunAsync` picks the right procedure for the action, binds the
parameters, parses the message, and wraps the call in a transaction it **commits
only on success** — a failure flag or any exception rolls the whole process back.

```csharp
var result = await approvalProcess.RunAsync(
    ApprovalAction.Submit,
    query: $"UPDATE {table} SET APPROVE_STATUS = {status}, APPROVE_LEVEL = {level} WHERE ID = {id}",
    mainDocType: mainDocType, docType: docType, transId: id,
    approveStatus: status, userId: userId,
    orgId: org, compId: comp, branchId: branch, docDate: docDate);

if (!result.Success) return BadRequest(result.Message);
```

> If the procedure issues its own `COMMIT` internally, that commit ends the
> transaction — the rollback here can only undo work that is still pending. Verify
> your procedure leaves the commit to the caller.

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
