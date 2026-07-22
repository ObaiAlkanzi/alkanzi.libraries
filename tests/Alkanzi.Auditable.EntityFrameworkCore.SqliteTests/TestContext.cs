using Alkanzi.Auditable.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.Tests;

public class Budget : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}

/// <summary>An entity that does not participate in auditing at all.</summary>
public class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class StubUserProvider : IAuditUserProvider
{
    public int? UserId { get; set; } = 42;

    public int? GetCurrentUserId() => UserId;
}

public sealed class TestDbContext : DbContext
{
    private readonly AuditableSaveChangesInterceptor _interceptor;

    public TestDbContext(DbContextOptions<TestDbContext> options, AuditableSaveChangesInterceptor interceptor)
        : base(options)
    {
        _interceptor = interceptor;
    }

    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Currency> Currencies => Set<Currency>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(_interceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyAuditableQueryFilters();
}

/// <summary>
/// A real SQLite database, held open for the lifetime of the fixture.
/// In-memory SQLite is dropped as soon as the last connection closes, so the
/// connection is owned here rather than by the context.
/// </summary>
public sealed class SqliteFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public StubUserProvider UserProvider { get; } = new();

    public SqliteFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public TestDbContext CreateContext(AuditableOptions? options = null)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection);
        var interceptor = new AuditableSaveChangesInterceptor(UserProvider, options ?? new AuditableOptions());
        return new TestDbContext(builder.Options, interceptor);
    }

    public void Dispose() => _connection.Dispose();
}
