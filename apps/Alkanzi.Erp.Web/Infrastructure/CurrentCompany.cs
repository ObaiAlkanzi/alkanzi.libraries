using Alkanzi.ErpServices;

namespace Alkanzi.Erp.Web.Infrastructure;

/// <summary>
/// Supplies the tenant approvals are scoped to.
/// </summary>
/// <remarks>
/// TODO: read the real org / company / branch from the signed-in user's session
/// or tenant selection. The fixed values here match the Oracle tests' tenant so
/// the sample page resolves live rows before multi-tenancy is wired.
/// </remarks>
public sealed class CurrentCompany : IErpCompanyContext
{
    public int ORG_ID => 21;
    public int COMP_ID => 6;
    public int? BRANCH_ID => 1;
}
