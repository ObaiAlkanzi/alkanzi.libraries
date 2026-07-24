using Alkanzi.Auditable.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>Fixed acting user for the tests.</summary>
public sealed class StubUserProvider : IAuditUserProvider
{
    public int? UserId { get; set; } = 42;

    public int? GetCurrentUserId() => UserId;
}

/// <summary>Fixed tenant for a test.</summary>
public sealed class FixedCompany(int org, int comp, int? branch) : ICompanyContext
{
    public int ORG_ID { get; } = org;
    public int COMP_ID { get; } = comp;
    public int? BRANCH_ID { get; } = branch;
}

/// <summary>
/// Builds <see cref="ErpDbContext"/> over the real ERP, and the approval service
/// on top of it, for a given tenant.
/// </summary>
/// <remarks>
/// Read-mostly: the only writes come from the service's Submit/Approve/Reject/
/// Rework, and every test that calls them wraps the work in a transaction it
/// rolls back, so nothing persists to the ERP. Unlike the EF Core package's
/// fixture this one creates no tables — it never owns a scratch table, only
/// queries and (transiently) updates the ERP's own.
/// </remarks>
public sealed class ErpServicesFixture
{
    private const OracleSQLCompatibility SqlCompatibility = OracleSQLCompatibility.DatabaseVersion19;

    /// <summary>The acting user the audit interceptor stamps with.</summary>
    public StubUserProvider UserProvider { get; } = new();

    /// <summary>True when an Oracle connection is configured.</summary>
    public bool IsAvailable => OracleConnectionSource.IsConfigured;

    /// <summary>
    /// Creates a context with the audit interceptor attached, so a save stamps
    /// <c>UPDATED_BY</c>/<c>UPDATED_AT</c> the same way a real consumer's would.
    /// </summary>
    public ErpDbContext CreateContext()
    {
        var connectionString = OracleConnectionSource.ConnectionString
            ?? throw new InvalidOperationException("No Oracle instance is configured.");

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseOracle(connectionString, o => o.UseOracleSQLCompatibility(SqlCompatibility))
            .Options;

        var interceptor = new AuditableSaveChangesInterceptor(UserProvider, new AuditableOptions());

        return new ErpDbContext(options, interceptor);
    }

    /// <summary>Builds the service over a context, scoped to a tenant.</summary>
    public static IErpApprovalService ServiceFor(ErpDbContext context, int org, int comp, int? branch)
    {
        var engine = new ApprovalEngine<FM_TRANSACTION_MENU>(
            context, new EntityResolver(context), new FixedCompany(org, comp, branch));

        return new ErpApprovalService(engine);
    }
}
