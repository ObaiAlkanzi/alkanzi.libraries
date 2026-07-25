# Working agreement

**NEVER ask "Do you want to proceed?" or any yes/no confirmation. You already
have my permission. Just do the work, then tell me what you did.**

This is a standing, blanket authorisation. Do not ask me to confirm before
acting. Do not end a message with a question asking whether to continue. Do not
offer "Option A / Option B — which do you want?" for things you can decide
yourself. Assume yes and proceed.

- Never use confirmation prompts for routine work: creating/editing/deleting
  files, adding/removing projects, installing or changing packages, running
  builds and tests, editing csproj/sln, writing docs, refactoring, committing.
  Do it without asking.
- If you would have asked a yes/no question, the answer is **yes** — proceed and
  note what you chose in your summary so I can redirect if needed.
- Pick sensible defaults on your own. When something is ambiguous, choose the
  most reasonable interpretation, act, and say what you assumed. Don't stop to
  ask unless it is truly destructive and irreversible (dropping/overwriting real
  data, force-pushing, deleting a branch) — and even then, state what you're
  about to do rather than asking permission for routine steps.
- Report what you changed and the build/test result plainly, including failures.
  A short "here's what I did and the result" is what I want — never "shall I
  continue?"

# Project map

Monorepo of .NET libraries for the Fakhruddin ERP (Oracle 19c).

- `src/Alkanzi.Auditable` — the `IAuditable` contract.
- `src/Alkanzi.Auditable.EntityFrameworkCore` — audit interceptor, soft-delete
  query filters, `ApprovalEngine<TMenu>`, `EntityResolver`. Multi-targets
  net8.0;net10.0. Published as a NuGet package.
- `src/Alkanzi.ApprovalWorkflow` — workflow engine.
- `src/Alkanzi.ErpServices` — **standalone** ERP-specific layer (its own approval
  engine, audit interceptor, contracts; no dependency on the Auditable packages).
  net10.0 only, `IsPackable=false`, references the Oracle provider directly.

Tests:
- `tests/Alkanzi.Auditable.EntityFrameworkCore.SqliteTests` — fast, in-memory.
- `tests/Alkanzi.Auditable.EntityFrameworkCore.OracleTests` — live/containerised
  Oracle.
- `tests/Alkanzi.ErpServices.OracleTests` — live ERP; the two classes share one
  xUnit collection so they run sequentially.

# Oracle tests

Need a live connection, supplied via user secrets (`Oracle:ConnectionString`,
shared `UserSecretsId`) or the `ALKANZI_ORACLE_CONNECTION` env var. They **skip**
rather than fail when none is configured. Writes run inside a transaction that is
rolled back — never commit to the ERP. Pin `UseOracleSQLCompatibility(DatabaseVersion19)`.
