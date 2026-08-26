using System.Data;
using System.Globalization;
using Alkanzi.DataAccess;
using Alkanzi.DemoDataImporter;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Modules_DataTables.CALL_MODULES;
using Modules_DataTables.IM_MODULES;
using Modules_DataTables.PM_MODULES;

// ---- config (args: [sqlDir] [connectionString]) ----
var sqlDir = args.Length > 0
    ? args[0]
    : @"C:\Users\Obai Hussain\source\repos\alkanzi.libraries\apps\Alkanzi.SearchEngine.Demo\sql";

var conn = args.Length > 1
    ? args[1]
    : "Server=(localdb)\\MSSQLLocalDB;Database=AlkanziSearchDemo;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Proves the live index interceptor: insert/update/delete a customer via EF and watch SEARCH_INDEX follow.
if (args.Contains("--verify-live", StringComparer.OrdinalIgnoreCase))
{
    VerifyLive(conn);
    return;
}

var jobs = new (string File, Type Clr, string Table)[]
{
    ("vendors.sql",         typeof(FM_SUPPLIER_MASTER), "FM_SUPPLIER_MASTER"),
    ("customer.sql",        typeof(FM_CUSTOMER_MASTER), "FM_CUSTOMER_MASTER"),
    ("purchaseOrders.sql",  typeof(IM_PURCHASE_ORDERS), "IM_PURCHASE_ORDERS"),
    ("callRegistraton.sql", typeof(CALL_REGISTERATION), "CALL_REGISTERATION"),
};

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;
using var ctx = new AppDbContext(options);

Console.WriteLine("Ensuring database exists…");
ctx.Database.EnsureCreated();

// Clear existing rows so the import is repeatable (no FKs between these tables).
foreach (var j in jobs)
{
    var n = ctx.Database.ExecuteSqlRaw($"DELETE FROM [{j.Table}]");
    Console.WriteLine($"Cleared {n,6} existing rows from {j.Table}");
}

foreach (var j in jobs)
{
    var path = Path.Combine(sqlDir, j.File);
    if (!File.Exists(path)) { Console.WriteLine($"!! missing {path}"); continue; }

    var loaded = Load(ctx, conn, path, j.Clr);
    Console.WriteLine($"Loaded  {loaded,6} rows into {j.Table}  (from {j.File})");
}

Console.WriteLine("Done.");
return;

// ---------------------------------------------------------------------------
static void VerifyLive(string conn)
{
    // Context WITH the interceptor (as the API configures it).
    var live = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(conn).AddInterceptors(new SearchIndexInterceptor()).Options;
    using var db = new AppDbContext(live);
    SearchIndexBuilder.EnsureTable(db);

    var marker = "__LIVE_TEST__" + Guid.NewGuid().ToString("N")[..8];

    // INSERT
    var c = new FM_CUSTOMER_MASTER { NAME = marker };
    db.FM_CUSTOMER_MASTER.Add(c);
    db.SaveChanges();
    Console.WriteLine($"Inserted customer id={c.ID} '{marker}'. Index rows: {IndexCount(conn, c.ID)} (expect 1)");

    // UPDATE
    c.NAME = marker + "_UPDATED";
    db.SaveChanges();
    Console.WriteLine($"Updated. Index title now: '{IndexTitle(conn, c.ID)}' (expect *_UPDATED)");

    // DELETE
    db.FM_CUSTOMER_MASTER.Remove(c);
    db.SaveChanges();
    Console.WriteLine($"Deleted. Index rows: {IndexCount(conn, c.ID)} (expect 0)");

    Console.WriteLine("Live-index verify done.");
}

static int IndexCount(string conn, long id)
{
    using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options);
    return db.SearchIndex.Count(d => d.EntityType == "customer" && d.EntityId == id);
}

static string IndexTitle(string conn, long id)
{
    using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options);
    return db.SearchIndex.Where(d => d.EntityType == "customer" && d.EntityId == id)
             .Select(d => d.Title).FirstOrDefault() ?? "(none)";
}

// ---------------------------------------------------------------------------
static int Load(AppDbContext ctx, string conn, string path, Type clr)
{
    var et = ctx.Model.FindEntityType(clr)
             ?? throw new InvalidOperationException($"No EF entity for {clr.Name}");
    var store = StoreObjectIdentifier.Table(et.GetTableName()!, et.GetSchema());

    // Columns EF actually created for this table (skip shadow/computed/unmapped).
    var props = et.GetProperties()
        .Where(p => !p.IsShadowProperty() && p.GetComputedColumnSql() == null)
        .Select(p => new ColSpec(
            PropName: p.Name,
            Column: p.GetColumnName(store) ?? p.Name,
            Clr: Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType,
            Nullable: p.IsNullable))
        .Where(c => IsSupported(c.Clr))
        .ToList();

    var dt = new DataTable();
    foreach (var c in props)
    {
        var col = new DataColumn(c.Column, c.Clr) { AllowDBNull = true };
        dt.Columns.Add(col);
    }

    var text = File.ReadAllText(path);
    foreach (var map in OracleInsertParser.Parse(text))
    {
        var row = dt.NewRow();
        foreach (var c in props)
        {
            map.TryGetValue(c.PropName, out var raw);
            row[c.Column] = ConvertValue(raw, c.Clr, c.Nullable);
        }
        dt.Rows.Add(row);
    }

    using var sql = new SqlConnection(conn);
    sql.Open();
    using var bulk = new SqlBulkCopy(sql, SqlBulkCopyOptions.KeepIdentity, null)
    {
        DestinationTableName = $"[{et.GetSchema() ?? "dbo"}].[{et.GetTableName()}]",
        BulkCopyTimeout = 600,
    };
    foreach (DataColumn c in dt.Columns) bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
    bulk.WriteToServer(dt);

    return dt.Rows.Count;
}

static bool IsSupported(Type t) =>
    t == typeof(string) || t == typeof(DateTime) || t == typeof(bool) || t == typeof(Guid)
    || t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
    || t == typeof(decimal) || t == typeof(double) || t == typeof(float) || t == typeof(DateTimeOffset)
    || t.IsEnum;

static object ConvertValue(object? raw, Type clr, bool nullable)
{
    if (raw is null)
        return nullable ? DBNull.Value : DefaultFor(clr);

    try
    {
        if (clr == typeof(string)) return raw.ToString() ?? (object)DBNull.Value;
        if (clr == typeof(DateTime)) return raw is DateTime d ? d : DateTime.Parse(raw.ToString()!, CultureInfo.InvariantCulture);
        if (clr == typeof(DateTimeOffset)) return raw is DateTime d2 ? new DateTimeOffset(d2, TimeSpan.Zero) : DateTimeOffset.Parse(raw.ToString()!, CultureInfo.InvariantCulture);
        if (clr == typeof(bool)) { var s = raw.ToString()!.Trim(); return s is "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase); }
        if (clr == typeof(Guid)) return Guid.Parse(raw.ToString()!);

        // numeric — parse tolerant of decimals like "0.0"
        var num = decimal.Parse(raw.ToString()!.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
        if (clr == typeof(int)) return (int)num;
        if (clr == typeof(long)) return (long)num;
        if (clr == typeof(short)) return (short)num;
        if (clr == typeof(byte)) return (byte)num;
        if (clr == typeof(decimal)) return num;
        if (clr == typeof(double)) return (double)num;
        if (clr == typeof(float)) return (float)num;
        if (clr.IsEnum) return Enum.ToObject(clr, (int)num);
    }
    catch { /* fall through to default */ }

    return nullable ? DBNull.Value : DefaultFor(clr);
}

static object DefaultFor(Type clr) =>
    clr == typeof(string) ? "" : Activator.CreateInstance(clr)!;

record ColSpec(string PropName, string Column, Type Clr, bool Nullable);
