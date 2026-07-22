# Alkanzi.Auditable.EntityFrameworkCore

Entity Framework Core integration for [`Alkanzi.Auditable`](https://www.nuget.org/packages/Alkanzi.Auditable). Stops you from having to remember `MarkCreated` / `MarkUpdated` / `MarkDeleted` at every call site:

- a **SaveChanges interceptor** that stamps every tracked `IAuditable` entity automatically,
- **soft delete** — `Remove(entity)` sets `IS_DELETED` instead of issuing a `DELETE`,
- **global query filters** so soft-deleted rows vanish from ordinary queries.

## Requirements
**.NET 8.0 or later**, EF Core 8 or later. Multi-targets `net8.0` and `net10.0`.

## Install
`dotnet add package Alkanzi.Auditable.EntityFrameworkCore`

## Setup

### 1. Tell the library who the current user is

```csharp
public sealed class HttpAuditUserProvider(IHttpContextAccessor accessor) : IAuditUserProvider
{
    public int? GetCurrentUserId()
    {
        var claim = accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
```

Returning `null` is fine — background jobs and seeding have no user. The interceptor falls back to `AuditableOptions.SystemUserId` (default `0`).

### 2. Register it

```csharp
services.AddAuditable<HttpAuditUserProvider>();

services.AddDbContext<AppDbContext>((sp, options) => options
    .UseOracle(connectionString)   // or UseSqlServer / UseNpgsql / UseMySql / UseSqlite
    .AddInterceptors(sp.GetRequiredService<AuditableSaveChangesInterceptor>()));
```

### 3. Add the query filters

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    // ... your own configuration ...

    modelBuilder.ApplyAuditableQueryFilters();   // last
}
```

That's it. Nothing changes in your entities — they just implement `IAuditable` as before.

## What you get

```csharp
var budget = new Budget { Name = "Marketing" };
context.Budgets.Add(budget);
await context.SaveChangesAsync();
// CREATED_BY = current user, CREATED_AT = UtcNow, IS_UPDATED/IS_DELETED = false

budget.Amount = 5000m;
await context.SaveChangesAsync();
// IS_UPDATED = true, UPDATED_BY, UPDATED_AT stamped

context.Budgets.Remove(budget);
await context.SaveChangesAsync();
// no DELETE — IS_DELETED = true, DELETED_BY, DELETED_AT stamped

await context.Budgets.CountAsync();                       // excludes it
await context.Budgets.IgnoreQueryFilters().CountAsync();  // includes it
```

## Options

```csharp
services.AddAuditable<HttpAuditUserProvider>(o =>
{
    o.SystemUserId = -1;    // stamped when no user is in scope; default 0
    o.SoftDelete = false;   // let DELETE through, but still stamp; default true
});
```

Skip filtering on specific types:

```csharp
modelBuilder.ApplyAuditableQueryFilters(type => type != typeof(AuditLogEntry));
```

## Things worth knowing

**Cascade deletes.** When you soft-delete a parent, EF has already marked its children `Deleted`. Auditable children are soft-deleted along with it; children that don't implement `IAuditable` are still **hard** deleted. If a relationship has a database-level `ON DELETE CASCADE`, the database never sees a `DELETE` at all, so those children survive — configure such relationships as `DeleteBehavior.Restrict` or soft-delete them explicitly.

**Existing query filters are not overwritten.** `ApplyAuditableQueryFilters` skips any entity type that already declares one — EF Core allows a single filter per type, and silently replacing yours would be worse than doing nothing. For those types, add the condition yourself:

```csharp
modelBuilder.Entity<Budget>().HasQueryFilter(b => b.TenantId == tenantId && b.IS_DELETED != true);
```

**Unique indexes.** A soft-deleted row still occupies its slot, so a unique index will reject a replacement with the same key. The fix is database-specific — see [Unique indexes by provider](#unique-indexes-by-provider) below.

**`ExecuteDelete` bypasses all of this.** `ExecuteDelete()` and `ExecuteUpdate()` run as SQL without a change tracker, so no interceptor sees them. Use `Remove` when you want the audit trail.

**Filters are `IS_DELETED != true`, not `== false`.** A row written before auditing existed may hold `NULL`, and those rows stay visible.

**Timestamps are UTC.** Stamping uses `DateTime.UtcNow`, from `IAuditable` itself. Convert only when displaying.

## Resolving entities by table name

When a table name lives in *data* rather than in code — a workflow row naming the table its transactions sit in — `IEntityResolver` turns that string into a row, using EF's model rather than a hand-maintained registry:

```csharp
services.AddEntityResolver<AppDbContext>();

var row = await resolver.FindAsync("FM_SOME_TABLE", transId);   // object?, or null
var clrType = resolver.GetEntityType("FM_SOME_TABLE").ClrType;
```

Matching is case-insensitive and accepts `TABLE` or `SCHEMA.TABLE`. Key values are coerced to the declared key type — passing an `int` where the key is `long` or `decimal` still finds the row, which matters on Oracle where `NUMBER` maps to either depending on precision.

Soft-deleted rows are excluded. `FindIncludingDeletedAsync` returns them, and has to issue its own `IgnoreQueryFilters()` query to do it: `Find` applies query filters to the SELECT it makes, so there is no way to opt out through `Find` itself.

### Dispatching on a document type

If your registry table has a document-type code and a table name, implement `ITransactionMenu` on it and `ApprovalEngine<TMenu>` will chain the two lookups:

```csharp
public class FM_TRANSACTION_MENU : BASE, ITransactionMenu   // your entity, your project
{
    public string DOC_TYPE { get; set; } = "";
    public string TABLE_NAME { get; set; } = "";
    // ... the rest of your columns are none of this package's business
}
```

```csharp
services.AddApprovalEngine<AppDbContext, FM_TRANSACTION_MENU>();

var menu = await engine.GetMenuAsync("PO");                             // the registry row
var txn  = await engine.GetTransactionByDocTypeAsync("PO", transId);    // the transaction
```

Only `DOC_TYPE` and `TABLE_NAME` are read, so the rest of your schema stays in your own solution. A document type with two registry rows raises rather than picking one, since silently choosing would route approvals by whichever row the database returned first.

## Database providers

The package is provider-agnostic. It emits no SQL of its own: the interceptor only manipulates the change tracker, and `ApplyAuditableQueryFilters` builds a LINQ expression that your provider translates.

The soft-delete and query-filter behaviour is verified end-to-end against **SQLite** and **Oracle 19c**. Other providers are expected to work but are not covered by tests.

A few things *are* worth knowing per database.

### How `IS_DELETED` is stored

`bool?` maps to whatever the provider considers a nullable boolean — `bit` on SQL Server, `boolean` on PostgreSQL, `NUMBER(1)` on Oracle, `tinyint(1)` on MySQL. You never write that comparison yourself, so it rarely matters; it only surfaces if you hand-write SQL or a raw index filter, where `IS_DELETED <> 1` is right on Oracle and `"IS_DELETED" IS NOT TRUE` is right on PostgreSQL.

### Oracle 19c: set the SQL compatibility level

**On Oracle 19c you must pin the provider's SQL compatibility, or table creation fails.**

`Oracle.EntityFrameworkCore` 23.x defaults to 23ai SQL and emits the **native `BOOLEAN` datatype** for `bool` properties. That datatype was introduced in Oracle 23ai and does not exist in 19c, so `IS_UPDATED` and `IS_DELETED` make any `CREATE TABLE` fail:

```
ORA-00902: invalid datatype
```

19c is the long-term-support release, so this hits most real deployments. Pin the compatibility level when configuring the context:

```csharp
options.UseOracle(
    connectionString,
    o => o.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));
```

That maps `bool?` to `NUMBER(1)` instead:

```diff
- "IS_UPDATED" BOOLEAN,      -- 23ai only; ORA-00902 on 19c
+ "IS_UPDATED" NUMBER(1),    -- valid on 19c, 21c and 23ai alike
```

`NUMBER(1)` is accepted by every supported Oracle version, so pinning to 19 is the safe default even if you later upgrade the server. This applies to your whole model, not just the audit columns — any `bool` property has the same problem.

### Oracle identifier casing

EF's Oracle provider **quotes** identifiers, which makes them case-sensitive: a `Budgets` entity becomes the table `"Budgets"`, and unquoted `BUDGETS` in hand-written SQL will not find it (ORA-00942).

The audit columns are unaffected — `IS_DELETED` and friends are already `UPPER_SNAKE_CASE`, so their quoted form is identical to what unquoted SQL folds to. It is *your* entity and property names that bite. Either map them to upper case explicitly:

```csharp
entity.ToTable("BUDGETS");
entity.Property(b => b.Code).HasColumnName("CODE");
```

…or quote them to match EF exactly in any raw SQL you write.

### Unique indexes by provider

Soft-deleted rows keep occupying their key, so a plain unique index blocks reusing a code from a deleted record. Only some databases can express "unique among non-deleted rows" as an index.

| Provider | Supports it | How |
| --- | --- | --- |
| SQL Server | Yes — filtered index | `.HasFilter("[IS_DELETED] IS NULL OR [IS_DELETED] <> 1")` |
| PostgreSQL | Yes — partial index | `.HasFilter("\"IS_DELETED\" IS NOT TRUE")` |
| SQLite | Yes — partial index | `.HasFilter("IS_DELETED IS NULL OR IS_DELETED <> 1")` |
| Oracle | No filtered indexes | Function-based unique index, below |
| MySQL / MariaDB | No filtered indexes | Generated column, below |

```csharp
modelBuilder.Entity<Budget>()
    .HasIndex(b => b.Code)
    .IsUnique()
    .HasFilter("[IS_DELETED] IS NULL OR [IS_DELETED] <> 1");   // SQL Server
```

Include the `IS NULL` arm. A bare `<> 1` silently drops rows where `IS_DELETED` is `NULL` out of the index, so legacy rows written before auditing existed would escape the uniqueness check — while the query filter still treats them as live. The two must agree on what "not deleted" means.

**Oracle** has no filtered indexes, but its B-tree indexes skip entries whose key columns are entirely `NULL`. A function-based unique index that maps deleted rows to `NULL` gets the same effect, added via raw SQL in a migration:

```sql
CREATE UNIQUE INDEX UX_BUDGETS_CODE_ACTIVE ON BUDGETS (
    CASE WHEN IS_DELETED = 1 THEN NULL ELSE CODE END
);
```

Mind the identifier casing. EF's Oracle provider quotes identifiers, so a `Budgets` entity becomes the case-sensitive table `"Budgets"` — and the unquoted `BUDGETS` above will not find it. Either map the table and columns to upper case (`ToTable("BUDGETS")`, `HasColumnName("CODE")`) or quote them in the DDL to match EF exactly. The audit columns are already `UPPER_SNAKE_CASE`, so `IS_DELETED` needs no special handling; it is your own columns that bite.

**MySQL and MariaDB** have neither, but permit repeated `NULL`s in a unique index. Same trick through a generated column:

```sql
ALTER TABLE Budgets
    ADD Code_Active VARCHAR(50) AS (IF(IS_DELETED = 1, NULL, Code)) STORED,
    ADD UNIQUE INDEX UX_Budgets_Code_Active (Code_Active);
```

If none of this fits, drop the database constraint and enforce uniqueness in application code — but be aware that concurrent inserts can then race past the check.

## License
MIT
