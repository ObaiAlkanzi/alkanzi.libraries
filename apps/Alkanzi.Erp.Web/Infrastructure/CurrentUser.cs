using Alkanzi.ErpServices;

namespace Alkanzi.Erp.Web.Infrastructure;

/// <summary>
/// Supplies the acting user the audit interceptor stamps approvals with.
/// </summary>
/// <remarks>
/// TODO: read the real user id from the authenticated principal — e.g. inject
/// <c>AuthenticationStateProvider</c> and map a claim to the ERP user id. The
/// fixed value here matches the Oracle tests' acting user so the sample page
/// works before auth is wired.
/// </remarks>
public sealed class CurrentUser : IErpUserProvider
{
    public int? GetCurrentUserId() => 42;
}
