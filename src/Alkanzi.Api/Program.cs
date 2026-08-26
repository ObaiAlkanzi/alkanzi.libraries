using Alkanzi.Application;
using Alkanzi.DataAccess;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=AlkanziSearchDemo;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Composition root: application use cases + infrastructure (data + search adapters).
builder.Services.AddAlkanziApplication();
builder.Services.AddAlkanziDataAccess(connectionString);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Let the AngularJS demo (a separate origin) call the API during local dev.
const string DemoCors = "DemoCors";
builder.Services.AddCors(options => options.AddPolicy(DemoCors, p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Create + seed the local database on startup. LocalDB can be slow/stuck to spin up
// (error 50 "process failed to start"); retry a few times, and never let a DB hiccup
// crash the whole API at boot — the app still starts so you can fix LocalDB and retry.
using (var scope = app.Services.CreateScope())
{
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    const int maxAttempts = 6;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            DemoDataSeeder.Seed(db);
            // Unified search index: create the table if missing (no drop) and build it once.
            SearchIndexBuilder.EnsureTable(db);
            if (SearchIndexBuilder.Count(db) == 0)
            {
                var n = SearchIndexBuilder.Rebuild(db);
                log.LogInformation("Search index built: {Count} documents.", n);
            }
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            log.LogWarning("Database not ready (attempt {Attempt}/{Max}) - is LocalDB starting? {Msg}", attempt, maxAttempts, ex.Message);
            Thread.Sleep(3000);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Database init failed. The API still starts; run 'sqllocaldb start MSSQLLocalDB', then POST /api/search/reindex.");
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(DemoCors);
app.MapControllers();

// Rebuild the unified search index on demand (after data changes / re-import).
app.MapPost("/api/search/reindex", (AppDbContext db) =>
{
    var n = SearchIndexBuilder.Rebuild(db);
    return Results.Ok(new { indexed = n });
});

app.Run();
