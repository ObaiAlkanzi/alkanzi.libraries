using System.Security.Claims;
using Alkanzi.Erp.Application.Abstractions;

namespace Alkanzi.Erp.Infrastructure;

/// <summary>
/// Resolves the acting user from the request cookie — the web app's implementation of the
/// application's <see cref="ICurrentUser"/> port.
/// <para>
/// Company and branch are read from claims stamped at sign-in rather than looked up per
/// request. They are stable for the life of a session, and re-querying them on every call
/// would put a database round trip in front of work that has not started yet.
/// </para>
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    public const string CompanyIdClaim = "erp:company_id";
    public const string BranchIdClaim = "erp:branch_id";

    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public int CompanyId =>
        int.TryParse(Principal?.FindFirstValue(CompanyIdClaim), out var id) ? id : 0;

    public int? BranchId =>
        int.TryParse(Principal?.FindFirstValue(BranchIdClaim), out var id) ? id : null;
}
