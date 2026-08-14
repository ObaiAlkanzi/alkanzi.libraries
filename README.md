# Alkanzi.Libraries

Development monorepo for the Alkanzi .NET packages. Both libraries build, version, and test together here; each still ships as its own NuGet package.

## Packages

| Package | Description | NuGet |
| --- | --- | --- |
| [`Alkanzi.Auditable`](src/Alkanzi.Auditable) | Interface-based audit-stamping contract (`IAuditable`) — Created/Updated/Deleted metadata for any entity, no base class required. | [![NuGet](https://img.shields.io/nuget/v/Alkanzi.Auditable.svg)](https://www.nuget.org/packages/Alkanzi.Auditable) |
| [`Alkanzi.Auditable.EntityFrameworkCore`](src/Alkanzi.Auditable.EntityFrameworkCore) | EF Core integration for `IAuditable` — a SaveChanges interceptor that stamps entities automatically, soft delete, and global query filters. | [![NuGet](https://img.shields.io/nuget/v/Alkanzi.Auditable.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Alkanzi.Auditable.EntityFrameworkCore) |
| [`Alkanzi.ApprovalWorkflow`](src/Alkanzi.ApprovalWorkflow) | Generic multi-level approval workflow engine — attach an ordered approval chain to any entity type. | [![NuGet](https://img.shields.io/nuget/v/Alkanzi.ApprovalWorkflow.svg)](https://www.nuget.org/packages/Alkanzi.ApprovalWorkflow) |

`Alkanzi.Auditable.EntityFrameworkCore` and `Alkanzi.ApprovalWorkflow` both depend on `Alkanzi.Auditable`. Inside this repo those links are `ProjectReference`s, so changes flow across projects immediately — no NuGet round-trip while developing. `dotnet pack` still emits them as proper package dependencies.

## Layout

```
Alkanzi.Libraries.slnx
├─ src/
│  ├─ Alkanzi.Auditable/                          # IAuditable contract
│  ├─ Alkanzi.Auditable.EntityFrameworkCore/      # EF Core interceptor + filters (-> Auditable)
│  └─ Alkanzi.ApprovalWorkflow/                   # workflow engine (-> Auditable)
└─ tests/
   ├─ Alkanzi.ApprovalWorkflow.Tests/                    # xUnit, covers Auditable + ApprovalWorkflow
   ├─ Alkanzi.Auditable.EntityFrameworkCore.SqliteTests/ # xUnit against in-memory SQLite (fast, seeded)
   └─ Alkanzi.Auditable.EntityFrameworkCore.OracleTests/ # Testcontainers or a live ERP (needs Docker)
```

## Oracle integration tests

`Alkanzi.Auditable.EntityFrameworkCore.OracleTests` runs against a real Oracle instance via [Testcontainers](https://dotnet.testcontainers.org/), covering the provider-specific behaviour SQLite cannot express — chiefly the function-based unique index that lets a code be reused after its holder is soft-deleted.

These tests need an Oracle instance. Without one they **skip** rather than fail, so `dotnet test` stays green on machines that cannot run containers:

```
Skipped! - Failed: 0, Passed: 0, Skipped: 6
   No Oracle available — start Docker, or set ALKANZI_ORACLE_CONNECTION to an Oracle connection string.
```

**Treat a skipped run as "unverified", not "passing".** There are two ways to make them run.

### With Docker (default)

Start Docker and run the tests. The first run pulls `gvenzl/oracle-free:23-slim-faststart` (~2 GB) and takes a few minutes; later runs reuse the cached image and share one container across the whole assembly.

Set `ALKANZI_FORCE_ORACLE_TESTS=1` on CI images where the Docker CLI is absent but a daemon is reachable through `DOCKER_HOST`.

### Against an existing Oracle instance

Point the tests at a database you already have — no Docker required:

```powershell
$env:ALKANZI_ORACLE_CONNECTION = "User Id=dev;Password=***;Data Source=localhost:1521/FREEPDB1"
dotnet test tests/Alkanzi.Auditable.EntityFrameworkCore.OracleTests
```

**Use a disposable schema, never production.** The fixture creates, truncates and drops a table on the target. It is confined to one table named `ALKANZI_TEST_BUDGETS` — deliberately not `BUDGETS`, which a real ERP schema may well own — and it drops that table again on the way out, but the account you connect with should still be a throwaway dev user with no access to anything you care about.

The fixture pins `UseOracleSQLCompatibility(DatabaseVersion19)`, because the provider's 23ai default emits a `BOOLEAN` datatype that Oracle 19c rejects. See the [package README](src/Alkanzi.Auditable.EntityFrameworkCore/README.md#oracle-19c-set-the-sql-compatibility-level) — this affects consumers too, not just tests.

## Requirements
**.NET 8.0 or later.** The libraries multi-target `net8.0` and `net10.0`; the test project runs on `net10.0`. `IAuditable` uses C# default interface methods, so .NET Standard 2.0 and .NET Framework are not supported.

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
```

## Running the ERP web app in Docker

`apps/Alkanzi.Erp.Web` (Blazor Server) ships a Dockerfile and a root `docker-compose.yml`.

```bash
cp .env.example .env      # then fill in ERP_CONNECTION_STRING
docker compose up --build
```

The app listens on <http://localhost:8080>.

A few things worth knowing before you change any of it:

- **The build context is the repo root**, not the app folder — the app has a `ProjectReference` into `src/Alkanzi.ErpServices`, so a context rooted at the app cannot see it. Build by hand with `docker build -f apps/Alkanzi.Erp.Web/Dockerfile -t alkanzi-erp-web:local .`; compose already does this for you.
- **The connection string is never baked into the image.** It arrives as `ConnectionStrings__Erp`, read from `ERP_CONNECTION_STRING` in the git-ignored `.env`. Building needs no secret; only running does. Start it without one and the app stops immediately, naming the missing key.
- **Not the `-alpine` runtime, and no invariant globalization.** Oracle's managed ADO.NET driver needs a real ICU and throws on a runtime without one.
- **Data Protection keys live in a named volume.** Blazor Server signs antiforgery tokens and circuit state with them; drop the volume and every restart invalidates open pages until users hard-refresh.
- **`/healthz` is liveness only** — it does not touch Oracle, so an ERP blip cannot cause Docker to restart an otherwise healthy web app. `docker compose ps` reports the state.

## Packing

```bash
dotnet pack -c Release -o ./artifacts
```

Produces a `.nupkg` for each library. Versions are set per-project via `<Version>` in the respective `.csproj`.

## Publishing

```bash
dotnet nuget push ./artifacts/Alkanzi.Auditable.<version>.nupkg \
  --source https://api.nuget.org/v3/index.json --api-key <key>
```

Publish `Alkanzi.Auditable` first when several have changed — the other two declare a dependency on it, and NuGet.org will reject a package whose dependency version doesn't yet exist.

## Related repositories
The original single-package repos remain as published history:
[alkanzi.auditable](https://github.com/ObaiAlkanzi/alkanzi.auditable) ·
[alkanzi.approvalworkflow](https://github.com/ObaiAlkanzi/alkanzi.approvalworkflow)

## License
MIT — see [LICENSE](LICENSE).
