using Alkanzi.Auditable.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore.SqliteTests;

public class Budget : IAuditable, IApprovable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // Approvable but not workflow-bound, mirroring the common shape in the ERP:
    // APPROVE_STATUS/LEVEL are near-universal, WORKFLOW_ID is not.
    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }

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

/// <summary>A transaction carrying both approval columns and a workflow id.</summary>
public class WorkOrder : IAuditable, IApprovable, IWorkflowBound
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }
    public int? WORKFLOW_ID { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}

/// <summary>
/// Stands in for the ERP's FM_TRANSACTION_MENU: a document-type registry whose
/// rows name the table their transactions live in.
/// </summary>
public class FM_TRANSACTION_MENU : ITransactionMenu
{
    public int ID { get; set; }

    // Initialised because the properties are non-nullable, so EF maps them NOT
    // NULL and a seed that sets only DOC_TYPE/TABLE_NAME fails to insert.
    // Mark any column that is genuinely nullable in Oracle as `string?`.
    public string DISPLAY_NAME { get; set; } = string.Empty;
    public string MAIN_DOC_TYPE { get; set; } = string.Empty;
    public int FROM { get; set; }
    public int TO { get; set; }
    public int CURR_NO { get; set; }
    public string TRANSACTION_TYPE { get; set; } = string.Empty;
    public string PREFIX { get; set; } = string.Empty;
    public int PENDING { get; set; }
    public int SUBMIT { get; set; }
    public int REWORK { get; set; }
    public decimal PENALTY_AMOUNT { get; set; }
    public bool STATUS { get; set; }
    public bool MULTI_WF { get; set; } 
    public string DOC_TYPE { get; set; } = string.Empty;

    // Settable auto-properties rather than the generated `=> throw` stubs: EF
    // must read and write these, and the registry lookup is keyed by them.
    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int? BRANCH_ID { get; set; }

    // Nullable, and null on most real rows: plenty of document types configure
    // numbering and workflow without naming a transaction table.
    public string? TABLE_NAME { get; set; }
}

/// <summary>Fixed tenant for tests.</summary>
public sealed class StubCompanyContext : ICompanyContext
{
    public int ORG_ID { get; set; } = 21;
    public int COMP_ID { get; set; } = 6;
    public int? BRANCH_ID { get; set; } = 1;
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
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<FM_TRANSACTION_MENU> Menus => Set<FM_TRANSACTION_MENU>();

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
