using Alkanzi.Auditable.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.EntityFrameworkCore.Infrastructure;
using Testcontainers.Oracle;

namespace Alkanzi.Auditable.EntityFrameworkCore.OracleTests;

public class Budget : IAuditable
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}

public sealed class StubUserProvider : IAuditUserProvider
{
    public int? UserId { get; set; } = 42;

    public int? GetCurrentUserId() => UserId;
}

public sealed class OracleDbContext(
    DbContextOptions<OracleDbContext> options,
    AuditableSaveChangesInterceptor interceptor) : DbContext(options)
{
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(interceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Upper-case table and column names on purpose. EF's Oracle provider
        // quotes identifiers, and a quoted "BUDGETS" matches what unquoted
        // BUDGETS folds to — which is what lets the hand-written DDL in the
        // README refer to these objects without quoting.
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable(OracleFixture.TableName);
            entity.Property(b => b.Id).HasColumnName("ID");
            entity.Property(b => b.Code).HasColumnName("CODE").HasMaxLength(50);
            entity.Property(b => b.Name).HasColumnName("NAME").HasMaxLength(200);
        });

        modelBuilder.ApplyAuditableQueryFilters();
    }
}

/// <summary>
/// One Oracle container for the whole assembly — startup is measured in tens of
/// seconds, so it is shared and each test clears the table instead.
/// </summary>
public sealed class OracleFixture : IAsyncLifetime
{
    /// <summary>
    /// Deliberately not <c>BUDGETS</c>. When these tests run against a shared
    /// instance (see <see cref="ConnectionStringVariable"/>) the fixture creates,
    /// truncates and drops this table, so its name must be one no real schema
    /// would use. An ERP schema plausibly owns a table called <c>BUDGETS</c>.
    /// </summary>
    public const string TableName = "ALKANZI_TEST_BUDGETS";

    /// <summary>
    /// Environment variable holding an Oracle connection string, used to run
    /// against an existing instance instead of a throwaway container.
    /// User secrets work too — see <see cref="OracleConnectionSource"/>.
    /// </summary>
    public const string ConnectionStringVariable = OracleConnectionSource.EnvironmentVariable;

    private const string IndexName = "UX_ALKANZI_TEST_BUDGETS_ACTIVE";

    /// <summary>
    /// Targets Oracle 19c SQL, which maps <c>bool</c> to <c>NUMBER(1)</c>.
    /// </summary>
    /// <remarks>
    /// Not the provider default. Provider 23.x defaults to 23ai SQL and emits
    /// the native <c>BOOLEAN</c> datatype, which 19c rejects outright with
    /// ORA-00902 — and 19c is the long-term-support release most deployments
    /// still run. <c>NUMBER(1)</c> is valid on 19c, 21c and 23ai alike, so
    /// pinning to 19 tests the mapping real consumers will actually have.
    /// </remarks>
    private const OracleSQLCompatibility SqlCompatibility = OracleSQLCompatibility.DatabaseVersion19;

    private OracleContainer? _container;
    private string? _connectionString;

    public StubUserProvider UserProvider { get; } = new();

    public bool IsAvailable => _connectionString is not null;

    public async Task InitializeAsync()
    {
        if (OracleConnectionSource.IsConfigured)
        {
            _connectionString = OracleConnectionSource.ConnectionString;
        }
        else if (DockerProbe.IsAvailable)
        {
            // Pinned rather than floating: an Oracle major version bump should be
            // a deliberate change, not something a CI run picks up on its own.
            _container = new OracleBuilder("gvenzl/oracle-free:23-slim-faststart").Build();
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }
        else
        {
            // Leave null; every test carrying [DockerFact] is skipped.
            return;
        }

        await using var context = CreateContext();

        // Drop first: a borrowed instance may still hold the table from an
        // interrupted run, and we would otherwise build on its stale shape.
        await DropIfPresentAsync(context);

        // CreateTables, not EnsureCreated. EnsureCreated no-ops when the schema
        // already holds tables, so on a real schema it would silently create
        // nothing and the CREATE INDEX below would fail with ORA-00942.
        // CreateTables issues DDL for this context's model unconditionally —
        // and that model is the single test entity, nothing else.
        var creator = context.GetService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync();

        // The workaround the README prescribes for Oracle, which has no
        // filtered indexes: deleted rows collapse to NULL, and Oracle's B-tree
        // does not store an entry whose key columns are all NULL.
        await context.Database.ExecuteSqlRawAsync($"""
            CREATE UNIQUE INDEX {IndexName} ON {TableName} (
                CASE WHEN IS_DELETED = 1 THEN NULL ELSE CODE END
            )
            """);
    }

    public OracleDbContext CreateContext(AuditableOptions? options = null)
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException("No Oracle instance is configured.");
        }

        var builder = new DbContextOptionsBuilder<OracleDbContext>()
            .UseOracle(_connectionString, o => o.UseOracleSQLCompatibility(SqlCompatibility));

        return new OracleDbContext(
            builder.Options,
            new AuditableSaveChangesInterceptor(UserProvider, options ?? new AuditableOptions()));
    }

    /// <summary>Clears state between tests sharing the instance.</summary>
    public async Task ResetAsync()
    {
        UserProvider.UserId = 42;

        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync($"DELETE FROM {TableName}");
    }

    /// <summary>
    /// Drops the test table if it exists. ORA-00942 (table not found) is the
    /// expected first-run path and is swallowed; anything else propagates.
    /// </summary>
    private static Task DropIfPresentAsync(OracleDbContext context)
        => context.Database.ExecuteSqlRawAsync($"""
            BEGIN
                EXECUTE IMMEDIATE 'DROP TABLE {TableName} CASCADE CONSTRAINTS';
            EXCEPTION
                WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
            END;
            """);

    public async Task DisposeAsync()
    {
        // Tidy up only on a borrowed instance — a container is discarded whole.
        if (_container is null && _connectionString is not null)
        {
            await using var context = CreateContext();
            await DropIfPresentAsync(context);
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class OracleCollection : ICollectionFixture<OracleFixture>
{
    public const string Name = "Oracle";
}
