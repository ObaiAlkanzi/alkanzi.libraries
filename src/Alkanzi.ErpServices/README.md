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
  type, dispatched to its transaction table for the current tenant.
- **`ErpProcedureService`** — call Oracle stored procedures and functions over
  the same context connection.

It is an application library, not a NuGet package (`IsPackable=false`), and
single-targets `net10.0`.

## Layout

```
ErpContracts.cs                 IErpAuditable / IErpApprovable / IErpWorkflowBound
                                IErpTransactionMenu / IErpUserProvider / IErpCompanyContext
                                ApprovalAction
ErpEntities.cs                  FM_TRANSACTION_MENU, FM_JOURNAL_HDR, CALL_REGISTERATION
ErpDbContext.cs                 maps them, own soft-delete filters, attaches the interceptor
ErpAuditSaveChangesInterceptor.cs   stamps IErpAuditable, turns deletes into soft deletes
IErpApprovalEngine.cs           Get / Submit / Approve / Reject / Rework / ApplyApproval
ErpApprovalEngine.cs              dispatch (docType -> table -> CLR type -> row) + transition
IErpProcedureService.cs         ExecuteAsync / QueryAsync / ExecuteScalarProcAsync
ErpProcedureService.cs            over ErpDbContext's connection
ServiceCollectionExtensions.cs  AddErpApprovalEngine / AddErpProcedureService
```

## Wiring it up

The approval engine saves through the context, so the audit interceptor must be
attached to `ErpDbContext` for approvals to stamp `UPDATED_BY` / `UPDATED_AT`.

```csharp
// Your own tenant + acting-user implementations.
services.AddErpApprovalEngine<CurrentUser, CurrentCompany>();
services.AddErpProcedureService();

services.AddDbContext<ErpDbContext>((sp, options) => options
    .UseOracle(connectionString, o => o.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19))
    .AddInterceptors(sp.GetRequiredService<ErpAuditSaveChangesInterceptor>()));
```

Where `CurrentUser : IErpUserProvider` returns the acting user id and
`CurrentCompany : IErpCompanyContext` supplies `ORG_ID` / `COMP_ID` / `BRANCH_ID`.

> **Oracle 19c:** pin `UseOracleSQLCompatibility(DatabaseVersion19)`. The 23.x
> provider defaults to 23ai SQL and emits the native `BOOLEAN` type, which 19c
> rejects with ORA-00902.

## Approvals

```csharp
public sealed class JournalController(IErpApprovalEngine approvals)
{
    public Task Submit(int id)  => approvals.SubmitAsync("JournalVoucher", id);
    public Task Approve(int id) => approvals.ApproveAsync("JournalVoucher", id);
    public Task Rework(int id)  => approvals.ReworkAsync("JournalVoucher", id, targetLevel: 1);
}
```

Each call resolves the document type through `FM_TRANSACTION_MENU` (scoped to the
current tenant), resolves its `TABLE_NAME` to a mapped CLR type through EF's
model, loads the row, applies the transition, and saves:

| Action  | `APPROVE_STATUS` | `APPROVE_LEVEL`     |
|---------|------------------|---------------------|
| Submit  | 1                | +1                  |
| Rework  | 2                | = `targetLevel`     |
| Reject  | 3                | unchanged           |
| Approve | 4                | +1                  |

Dispatch is dynamic: a new transaction table needs only to be mapped on
`ErpDbContext`, not named in the engine. A table with no approval columns (not
`IErpApprovable`) is reported rather than blindly cast; a missing or soft-deleted
row is an error, not a no-op.

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
connection the tests **skip** rather than fail. The two test classes share one
xUnit collection so they run sequentially — concurrent Oracle connections make
REF CURSOR reads flaky.

- Approval tests dispatch and transition real rows, each inside a transaction
  that is rolled back — nothing persists to the ERP.
- Procedure tests use anonymous PL/SQL blocks over `dual`, so their assertions
  are deterministic and depend on no schema object, while the binding and
  cursor-reading code is exactly what a real procedure call uses.

```bash
dotnet user-secrets --project tests/Alkanzi.ErpServices.OracleTests \
  set "Oracle:ConnectionString" "User Id=...;Password=...;Data Source=..."

dotnet test tests/Alkanzi.ErpServices.OracleTests
```
