# Flexion ERP search engine — plan and handoff

**Status:** design agreed, not started. No code written yet.
**Target project:** `src/Alkanzi.ErpServices` (the standalone ERP layer, net8.0;net10.0, Oracle 19c).
**Written:** 2026-08-14.

This file is the pick-up point. A fresh session on any machine should be able to
read this and start work without re-deriving anything. Answer the **blocking
questions** at the bottom first — they change the design.

---

## 1. Verdict: yes, this is buildable, and most of the machinery already exists

`ErpApprovalDashboardService` already does the structurally hard part of a search
engine over Flexion:

| What search needs | Where it already exists |
| --- | --- |
| doc type → which table holds its transactions | `LoadMenusAsync()` over `FM_TRANSACTION_MENU` |
| raw SQL built against a dynamic table name | `QueryTableAsync()` / `QueryUserTableAsync()` |
| identifier guarding against injection | `IsSafeTableName()` — live data has a `TABLE_NAME` with a stray quote + tab |
| user → which doc types they may see | `GetUserScopeAsync()` / `UserScopeSql` |
| one query per *table*, not per doc type | `GetUserDataAsync()` — `PRF_TRANSACTIONS` alone backs ~25 doc types |
| soft-delete exclusion | `(IS_DELETED IS NULL OR IS_DELETED != 1)` |
| tolerate a table missing expected columns | `catch (OracleException) → skip table` |
| chunked binds under Oracle's 1000-entry `IN` cap | `PairChunkSize = 250` |

A search service is that same machine with a different `WHERE` clause plus a
ranking step. **No new infrastructure is required for a working v1.**

---

## 2. Design — three tiers

Ship **tier 1** first. **Tier 2** is the target end state. **Tier 3** only on
explicit request.

### Tier 1 — metadata-driven live search (no DB objects, works day one)

`IErpSearchService.SearchAsync(request, ct)`:

1. Resolve the user's accessible doc types — reuse the `GetUserScopeAsync` query.
2. Group them by `TABLE_NAME` (one query per table, not per doc type).
3. Per table, build a `WHERE` from a **searchable-column map**:
   - numeric term → `DOC_NUM = :n OR ID = :n`
   - date-ish term → `DOC_DATE` range
   - text term → `UPPER(col) LIKE UPPER(:q)` over the configured text columns
4. Cap rows per table, then rank in C#: exact id > prefix match > contains, then
   recency.

The column list comes from `ALL_TAB_COLUMNS` at startup (cached), intersected
with a curated per-doc-type override. A table missing `REMARKS` just contributes
fewer predicates instead of throwing — same defensive posture as the dashboard.

- **Cost:** ~4 new files, no schema change, no DBA involvement.
- **Limit:** `LIKE '%x%'` is a full scan. Fine for filtered searches (doc number,
  date, party) and small/medium tables; painful on multi-million-row tables.

### Tier 2 — one denormalised index table + Oracle Text (recommended target)

A single table in **our** schema, never the vendor's:

```sql
SM_SEARCH_INDEX(
  ID, DOC_TYPE, TABLE_NAME, TRANS_ID,
  ORG_ID, COMP_ID, BRANCH_ID, WORKFLOW_ID,
  DOC_DATE, DOC_NUM, TITLE, SEARCH_BLOB CLOB,
  IS_DELETED, UPDATED_AT)
```

One `CONTEXT` index on `SEARCH_BLOB` gives real full-text across every module at
once: `CONTAINS(SEARCH_BLOB, 'fuzzy(acme) OR acme%', 1) > 0` with a relevance
`SCORE(1)`, stemming, sub-second. **One index to maintain instead of forty.**

Populated by an `ErpSearchIndexer` (per-doc-type `SELECT` → upsert), run on a
timer and/or triggered after approval actions, with `SYNC (ON COMMIT)` or a
scheduled sync.

- **Needs from the DBA:** `CREATE TABLE` in our schema; Oracle Text installed;
  `CTXAPP` role on the app user.
- Sits behind the **same `IErpSearchService` interface** as tier 1, so the UI
  never changes. Feature-flag the switch.

### Tier 3 — external engine (OpenSearch / Elastic)

Only if we want cross-language typo tolerance, faceting, and sub-100ms at scale,
*and* accept running another service. Oracle Text covers the realistic Flexion
workload — do not start here.

---

## 3. Build steps, in order

1. **`ErpSearchContracts.cs`** — `ErpSearchRequest` (query, doc-type filter, date
   range, approve-status filter, paging), `ErpSearchHit` (doc type, display name,
   trans id, doc num, doc date, title, snippet, score, approve status),
   `ErpSearchScope`.
2. **`ErpSearchMetadata.cs`** — reads `FM_TRANSACTION_MENU` + `ALL_TAB_COLUMNS`,
   caches the doc-type → (table, searchable columns) map, validates **every**
   identifier before it reaches SQL. This is the security-critical piece; it gets
   its own tests. Extend/reuse `IsSafeTableName` for column names too.
3. **`ErpSearchService.cs` (tier 1)** — term classification, per-table SQL,
   chunked binds, ranking, paging.
4. **DI** — `AddErpSearchService()` in `ServiceCollectionExtensions.cs`, same
   shape as the existing `AddErpApprovalDashboardService()`.
5. **Tests** — `tests/Alkanzi.ErpServices.OracleTests/ErpSearchOracleTests.cs`,
   joined to the existing xUnit collection so it runs sequentially, skipping when
   no connection is configured, all writes rolled back.
6. **UI** — global search box in the consuming host app with debounce +
   keyboard nav, deep-linking to the document.
7. **Tier 2** — index-table DDL script, `ErpSearchIndexer` refresh service, a
   `CONTAINS`-based query path behind the same interface, behind a feature flag.

---

## 4. Blocking questions — answer these first

Items 1–4 block real work. 5–9 have defaults (section 5) and can be corrected later.

1. **An Oracle connection on the working machine.** Neither
   `ALKANZI_ORACLE_CONNECTION` nor the user-secrets file for
   `alkanzi-auditable-efcore-oracle-tests` was present on the machine this plan
   was written on. Without one the code can be written but not verified against a
   single row of real Flexion data. Set one of:
   - user secret `Oracle:ConnectionString` under `UserSecretsId`
     `alkanzi-auditable-efcore-oracle-tests`, or
   - env var `ALKANZI_ORACLE_CONNECTION`.
2. **May we create objects in a schema?** A read-only account caps us at tier 1.
   If yes: which schema, and is Oracle Text available?
   ```sql
   SELECT comp_name, status FROM dba_registry WHERE comp_id = 'CONTEXT';
   SELECT * FROM user_role_privs WHERE granted_role = 'CTXAPP';
   ```
3. **Which doc types matter for v1?** "Search everything" is a bad v1. Name the
   5–10 doc types users actually hunt for (invoices? leases? call registrations?
   JVs?) and those get curated columns first.
4. **What should a search *hit* display?** A transaction row currently yields only
   `ID / DOC_DATE / APPROVE_STATUS / WORKFLOW_ID`. A user searching "ACME" expects
   the customer name, the amount, the property — which means joining master tables.
   - **(a)** What is `FM_TRANSACTION_MENU.MAP_FUN`? It is already mapped on the
     entity (`ErpEntities.cs`) but nothing reads it. If it names a function that
     produces a display mapping, it may solve the whole title/label problem.
   - **(b)** If not: the join map for the top doc types — which column points at
     which master table.
5. **Headers only, or line/detail rows too?** Finding an invoice by an item on one
   of its lines is a substantially bigger job.
6. **Is the security gate complete?** The dashboard scopes by security group →
   workflow level. Is there *also* row-level filtering by `ORG_ID` / `COMP_ID` /
   `BRANCH_ID` per user (some user-branch table)? And are there doc types with
   **no** workflow that users may still legitimately search? A search box that
   surfaces a document the user cannot open is a real leak — this must be
   confirmed, not inferred.
7. **Volume.** Row counts for the largest transaction tables, `PRF_TRANSACTIONS`
   especially. This single number decides how soon tier 2 is needed.
   ```sql
   SELECT table_name, num_rows FROM all_tables
   WHERE table_name IN (SELECT DISTINCT UPPER(TRIM(table_name)) FROM FM_TRANSACTION_MENU)
   ORDER BY num_rows DESC NULLS LAST;
   ```
8. **Language.** Is data entered in Arabic as well as English? It changes the
   lexer/collation choice (`NLS_SORT=BINARY_AI` vs Oracle Text `BASIC_LEXER`) and
   is much cheaper to set correctly than to retrofit.
9. **Consumer.** Blazor global search box only, or also a JSON endpoint for other
   clients?

---

## 5. Defaults if 5–9 go unanswered

- Headers only.
- English, accent- and case-insensitive.
- Existing security-group/workflow-level scoping as the sole gate.
- Blazor search box **plus** a minimal API endpoint.
- Tier 1 first; tier 2 once volume justifies it.

---

## 6. Constraints that must not be violated

- **Oracle 19c.** Keep `UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)`
  — the 23.x provider otherwise emits native `BOOLEAN` and 19c rejects it (ORA-00902).
- **Never commit to the live ERP from tests.** Writes run inside a transaction that
  is rolled back; Oracle tests *skip* rather than fail when no connection is set.
- **Every dynamic identifier is validated, never concatenated raw.** Table *and*
  column names. All values go through `OracleParameter` with `BindByName = true`.
- **`Alkanzi.ErpServices` stays standalone** — no dependency on the
  `Alkanzi.Auditable*` packages.
- **Multi-target `net8.0;net10.0`** must keep compiling against both Oracle
  provider majors.
