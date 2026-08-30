using System.Security.Claims;
using Alkanzi.Auditable.EntityFrameworkCore;

namespace Alkanzi.Erp.Api.Infrastructure;

/// <summary>
/// Supplies the acting user id to the audit interceptor from the current request.
/// <para>
/// Returns null when nobody is signed in — during seeding, a background job or a health
/// check — which is not an error: the interceptor falls back to
/// <see cref="AuditableOptions.SystemUserId"/>.
/// </para>
/// </summary>
public sealed class HttpAuditUserProvider : IAuditUserProvider
{
    private readonly IHttpContextAccessor _accessor;

    public HttpAuditUserProvider(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? GetCurrentUserId()
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;

        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }
}
