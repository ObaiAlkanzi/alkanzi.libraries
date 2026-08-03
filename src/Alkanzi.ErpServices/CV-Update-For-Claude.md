# CV update — instructions for Claude (web)

**What I want:** please update my attached CV with the two changes below, keeping my existing layout, fonts, and section order. Return the full updated CV.

---

## 1) Job title

Change my title from **"Senior Full Stack Developer"** to **"Senior Full Stack Engineer"** everywhere it appears (header, current role, any summary line).

> Note: keep "Senior" and "Full Stack" — do **not** shorten it to "Software Engineer" (that would drop the seniority and the full-stack signal).

---

## 2) Add this achievement under my **current role** (Experience section)

Add it as a sub-entry / bullet group under my current job — it is on-the-job work, not a personal side project.

**Alkanzi.\* — internal .NET NuGet library suite (approval workflow for an Oracle 19c ERP)**

- Designed and published a document **approval engine** and **dashboard/reporting service** driven entirely by **Oracle stored procedures and parameterized SQL** — resolves a document type to its table through a registry, then applies multi-level approval transitions, digital signatures, and audit/history logging in a single database transaction.
- Engineered the engine to run on the host application's own database connection with **no ORM/entity coupling**, using **dependency injection** for pluggable acting-user and tenant context.
- Authored companion packages: `Alkanzi.Auditable` and `Alkanzi.Auditable.EntityFrameworkCore` (EF Core audit interceptor + soft-delete query filters) and `Alkanzi.ApprovalWorkflow` (workflow engine).
- **Multi-targeted .NET 8 / .NET 10**, semantically versioned, distributed through a private **NuGet** feed, and covered by **live Oracle integration tests** (every write rolled back — never committed to production).

*Tech: C#, .NET 8/10, EF Core, Oracle 19c (ODP.NET), PL/SQL, NuGet, xUnit, Dependency Injection.*

---

## 3) Skills section (if present)

Make sure these keywords appear so applicant-tracking systems catch them:
`Oracle 19c`, `PL/SQL`, `.NET 8/10`, `C#`, `EF Core`, `NuGet`, `Dependency Injection`, `Full-Stack`.

---

### Shorter one-line version (if space is tight)

> Built and published an internal .NET NuGet suite (`Alkanzi.*`) powering ERP document approvals — an SQL/stored-procedure–driven approval engine, dashboard service, and EF Core auditing library; multi-targeted .NET 8/10, versioned on a NuGet feed, and integration-tested against Oracle 19c.
