using Alkanzi.Auditable.EntityFrameworkCore;
using Alkanzi.Erp.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// The audit interceptor stamps IAuditable entities and turns deletes into soft deletes.
// It emits no SQL of its own, so it is provider-agnostic — the same registration would
// work against Oracle or SQL Server.
builder.Services.AddAuditable<HttpAuditUserProvider>(o =>
{
    o.SoftDelete = true;
    o.SystemUserId = 0;   // stamped when nothing is signed in: seeding, jobs, health checks
});

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

builder.Services.AddDbContext<ErpDbContext>((sp, options) => options
    .UseNpgsql(connectionString)
    // PostgreSQL folds unquoted identifiers to lower case, so PascalCase names would have to
    // be quoted everywhere in hand-written SQL. Mapping to snake_case keeps the schema
    // idiomatic and psql-friendly without renaming anything in C#.
    .UseSnakeCaseNamingConvention()
    .AddInterceptors(sp.GetRequiredService<AuditableSaveChangesInterceptor>()));

builder.Services.AddScoped<SearchService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Development convenience: apply migrations and seed on start so a fresh clone runs against
// an empty database without extra steps. In any other environment migrations are a deploy
// step, not something the app does to itself at boot.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        await DevSeed.SeedAsync(db);
    }
    catch (Exception ex)
    {
        // A database that is not up yet must not stop the app from starting — the pages that
        // do not touch it still work, and the error is visible instead of a startup crash.
        log.LogError(ex, "Database initialisation failed. Is PostgreSQL running?");
    }
}

app.Run();
