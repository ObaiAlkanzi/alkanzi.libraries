namespace Alkanzi.Erp.Application.Abstractions;

/// <summary>
/// Who is acting, and what they are scoped to.
/// <para>
/// A port, not a service: the application layer needs the caller's identity but must not know
/// it arrives on an HTTP cookie. The web app implements it over <c>HttpContext</c>; a
/// background job implements it with a fixed system identity, and neither the use cases nor
/// the tests change.
/// </para>
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The signed-in user's id, or null when nobody is signed in.</summary>
    int? UserId { get; }

    /// <summary>Company the session is scoped to. 0 when unauthenticated.</summary>
    int CompanyId { get; }

    /// <summary>Default branch for documents this user raises, if they have one.</summary>
    int? BranchId { get; }
}
