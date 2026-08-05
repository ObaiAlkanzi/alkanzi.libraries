using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>Fixed acting user for the tests.</summary>
public sealed class StubUserProvider : IErpUserProvider
{
    public int? UserId { get; set; } = 42;

    public int? GetCurrentUserId() => UserId;
}

/// <summary>
/// Builds <see cref="ErpDbContext"/> over the real ERP, and the approval engine
/// on top of it, for a given tenant.
/// </summary>
/// <remarks>
/// Read-mostly: the only writes come from the engine's approval transitions, and
/// every test that triggers one wraps the work in a transaction it rolls back,
/// so nothing persists to the ERP.
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
    /// <c>UPDATED_BY</c>/<c>UPDATED_AT</c> the way a real consumer's would.
    /// </summary>
    /// <param name="userProvider">
    /// Acting user for the audit stamp. Defaults to the shared <see cref="UserProvider"/>.
    /// Pass a per-test provider to act as a different user without mutating the
    /// fixture — it is shared across the whole collection, so mutating
    /// <see cref="UserProvider"/> would leak into every other test.
    /// </param>
    public ErpDbContext CreateContext(IErpUserProvider? userProvider = null)
    {
        var connectionString = OracleConnectionSource.ConnectionString
            ?? throw new InvalidOperationException("No Oracle instance is configured.");

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseOracle(connectionString, o => o.UseOracleSQLCompatibility(SqlCompatibility))
            .Options;

        return new ErpDbContext(options, new ErpAuditSaveChangesInterceptor(userProvider ?? UserProvider));
    }

    /// <summary>
    /// Builds the engine over a context, with a stub acting user so level
    /// authorization has a real <c>usr</c>. Tenant comes from the row, not a context.
    /// </summary>
    /// <param name="userProvider">
    /// Acting user the engine reports to <c>LVL_AUTHORIZATION</c> and stamps on the
    /// approval log. Pass the SAME instance given to
    /// <see cref="CreateContext(IErpUserProvider?)"/> — otherwise the log row and the
    /// audit columns are written by two different users.
    /// </param>
    public static IErpApprovalEngine EngineFor(ErpDbContext context, int org, int comp, int? branch, IErpUserProvider? userProvider = null)
        => new ErpApprovalEngine(context, userProvider: userProvider ?? new StubUserProvider());
}

/// <summary>
/// Serialises the Oracle test classes and shares one fixture. Without it xUnit
/// runs the classes in parallel, and concurrent connections make the REF CURSOR
/// reads flaky.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ErpOracleCollection : ICollectionFixture<ErpServicesFixture>
{
    public const string Name = "ErpOracle";
}
