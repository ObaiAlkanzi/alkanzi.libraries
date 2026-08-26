# Alkanzi.DemoDataImporter

Loads the real ERP data (exported from Oracle as `Insert into … to_timestamp(…)` dumps)
into the SQL Server demo database used by `Alkanzi.Api` / the search demo.

It parses each Oracle `INSERT` (handling quoted strings, `to_timestamp(...)`, nulls and
Oracle `DD-MON-RR` dates), maps the columns to the EF-created tables, and **bulk-copies**
with identity preserved (original IDs kept).

## What it loads

| File | Table | Entity |
|------|-------|--------|
| `vendors.sql` | `FM_SUPPLIER_MASTER` | vendors |
| `purchaseOrders.sql` | `IM_PURCHASE_ORDERS` | inventory LPOs |
| `callRegistraton.sql` | `CALL_REGISTERATION` | calls |

The default source folder is
`apps/Alkanzi.SearchEngine.Demo/sql`, and the default target is the LocalDB demo database.

## Run

```bash
# uses defaults (demo/sql folder + LocalDB AlkanziSearchDemo)
dotnet run --project tools/Alkanzi.DemoDataImporter

# or override: [sqlDir] [connectionString]
dotnet run --project tools/Alkanzi.DemoDataImporter -- "C:\path\to\sql" "Server=…;Database=…"
```

It is **idempotent** — it clears the three tables first, then reloads, so you can run it
repeatedly. Start the API afterwards; its startup seeder only fills the tables when they are
empty, so it won't touch imported data.

## Notes

- Rows whose value count doesn't match the column count are skipped (a handful of malformed
  export rows, logged only by count).
- Columns present in the dump but not on the EF entity (Oracle-only columns) are ignored;
  columns on the entity but absent from a row get `NULL`/CLR defaults.
- To start clean instead: `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE AlkanziSearchDemo"`,
  then run the API once (creates the schema) and re-run this importer.
