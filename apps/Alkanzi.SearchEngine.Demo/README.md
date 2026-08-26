# Procurement Workspace — Demo

A local, SQL-Server–backed demo of the Alkanzi search engine, built as a faithful
reproduction of the FlexionERP **Procurement Workspace**: AngularJS + **DevExtreme 22.2.3**
(`angular.module('demo', ['dx'])`) using the ERP's own `asset-panel.css` / `listPopup.css`.

Reproduced components (wired to the demo's data): the `dx-toolbar` hero bar with the
omni-search box, the `ap-kpi-card` KPI strip, the Top Vendors `dx-chart`, the Explorer
`dx-data-grid` with view tabs, the omni-search `dx-popover` + `dx-list`, the `listPopup`
(kz-list-popup) drill-down, and the `alkanziFormPopup` (form-tab-panel-popup) detail form.

Frontend assets under `wwwroot/lib/devextreme`, `wwwroot/css/{asset-panel,listPopup,
alkanzi-grid-colors}.css` are copied from the ERP; `wwwroot/js/pw-framework.js` reproduces
the ERP's `listPopup` / `alkanziFormPopup` / `showAlert` shells.

## Architecture (Clean Architecture)

Dependencies point **inward** toward the application/core; infrastructure and UI are
outer rings wired only at the composition root (`Alkanzi.Api/Program.cs`).

```
Alkanzi.SearchEngine.Demo   Presentation (UI)  — AngularJS: omni-search, KPI strip, explorer, drawer
        │  HTTP (CORS)
        ▼
Alkanzi.Api                 Presentation (API) — thin controllers over the application services
        │  depends on
        ▼
Alkanzi.Application         Application (use cases) — ISearchService / IProcurementService,
        │                                              DTOs, and the ports (IProcurementRepository)
        │  depends on
        ▼
Alkanzi.SearchEngine        Core/Domain — engine + ISearchProvider (framework-free)

Alkanzi.DataAccess          Infrastructure — implements the ports (EF Core repository),
                            AppDbContext (SQL Server), and the search provider adapters
                            (LPO, Call, Vendor — all built on the core engine).
                            Depends on Application; referenced by Api only to compose the graph.
```

Key point: `Alkanzi.Api` and `Alkanzi.Application` never reference EF Core. Controllers
talk to interfaces; the concrete EF implementations live in `Alkanzi.DataAccess` and are
bound in `Program.cs`. Swap the database (or mock the repository in tests) without touching
the API or application layers.

The UI holds **no** data/EF references — it only calls the API. The API base URL
comes from `appsettings.json` → `Api:BaseUrl` (default `http://localhost:5080`).

## Run it

Two projects need to run. Easiest is to set **multiple startup projects** in Visual
Studio (`Alkanzi.Api` + `Alkanzi.SearchEngine.Demo`), or from a terminal:

```bash
# 1) API (creates + seeds the LocalDB database on first run)
dotnet run --project src/Alkanzi.Api --urls http://localhost:5080

# 2) UI
dotnet run --project apps/Alkanzi.SearchEngine.Demo --urls http://localhost:5054
```

Then open <http://localhost:5054>.

- **Omni-search** (top bar): type e.g. `gulf`, `5001`, `acme` — results are grouped
  by Purchase Order / Call / Vendor. Click a hit to open the detail drawer.
- **KPI strip**: counts from `/api/procurement/kpis`.
- **Explorer**: tabbed list (Purchase Orders / Calls / Vendors) with filter + paging.

## Notes

- The DB (`AlkanziSearchDemo` on `(localdb)\MSSQLLocalDB`) is created and seeded on
  API startup. Delete it to re-seed.
- `AppDbContext` is registered **transient** on purpose: the engine fans out to
  providers in parallel, and each needs its own context (EF Core isn't thread-safe).
