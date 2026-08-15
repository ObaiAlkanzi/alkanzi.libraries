# Alkanzi.SearchEngine

A provider-based, permission-aware search engine for the Fakhruddin ERP.

One query fans out across registered `ISearchProvider`s (one per entity family —
inventory LPOs, service LPOs, calls, requisitions, vendors …), and the engine merges,
ranks and pages the hits into a uniform shape the UI can render as *Type · Title ·
Subtitle* and route to the document.

## Concepts

| Type | Role |
|------|------|
| `ISearchEngine` / `SearchEngine` | Entry point. Runs permitted providers in parallel, merges, branch-filters, ranks, pages. A failing provider is skipped, not fatal. |
| `ISearchProvider` | Searches one entity family, returns `SearchHit`s. |
| `SearchQuery` | Term + optional type/date filters + paging. |
| `SearchScope` | The caller's permission context (branches, types). Applied to every result. |
| `SearchHit` | Uniform result: `EntityType, Id, Title, Subtitle, Score, BranchId, Metadata`. |
| `ISearchableTransaction` | Seam mirroring `TRANSACTION_BASE`; lets one generic provider serve every transaction entity. |
| `TransactionSearchProvider<T>` | Ready-made id / doc-number provider for any `ISearchableTransaction`. |

## Wiring

```csharp
services.AddAlkanziSearch();

// Once TRANSACTION_BASE declares ": ISearchableTransaction" (it already has every property):
services.AddSearchProvider(sp => new TransactionSearchProvider<IM_PURCHASE_ORDERS>(
    "inventory",
    () => sp.GetRequiredService<FakhruddinAppDbContext>().IM_PURCHASE_ORDERS,
    title: x => $"LPO-{x.ID}"));

// Entity-specific providers add name/text matching + party subtitles.
services.AddSearchProvider<VendorSearchProvider>();
```

```csharp
var result = await engine.SearchAsync(
    new SearchQuery { Term = "4008" },
    new SearchScope { UserId = uid, AllowedBranches = branches });
```

## Design notes

- **Ids/doc-numbers** match live (indexed). **Name/text** matching belongs in entity
  providers, backed by Oracle Text or a maintained index for scale.
- **Permissions are first-class** — the engine never returns a hit outside the
  caller's `SearchScope`.
- The engine is entity-agnostic; only providers know about entities.
