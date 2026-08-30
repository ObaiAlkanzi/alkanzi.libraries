using System.Security.Claims;
using Alkanzi.Erp.Infrastructure;
using Alkanzi.Erp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    /// <summary>Claim carrying the API bearer token issued alongside the sign-in cookie.</summary>
    public const string ApiTokenClaim = "erp:api_token";

    public const string FullNameClaim = "erp:full_name";

    /// <summary>
    /// Role that lands on the IT workspace after signing in.
    /// <para>
    /// Spelled out here rather than shared from the data layer, which this project no longer
    /// references. That is a deliberate duplication of a string, not of a rule: the API stays
    /// the authority on what the role may actually do, and this only decides which page to
    /// open first. If it ever drifts, the worst outcome is landing on the dashboard.
    /// </para>
    /// </summary>
    private const string SuperAdminRole = "Super Admin";

    private readonly ApiAuthClient _api;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApiAuthClient api, ILogger<AccountController> logger)
    {
        _api = api;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "Sign in";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["Title"] = "Sign in";
        if (!ModelState.IsValid) return View(model);

        // The API is the only thing that touches the database, so it is what verifies the
        // password. This app turns its answer into a cookie.
        var result = await _api.SignInAsync(model.Email, model.Password);

        if (!result.Succeeded || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sign-in failed.");
            return View(model);
        }

        var user = result.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(FullNameClaim, user.FullName),
            // No company, branch or organization claim: the API reads scope from the bearer
            // token it issued, so a copy here would be a second source of truth that a client
            // could edit and a server might be tempted to trust.
        };

        // Roles come from the API rather than from a local store, so [Authorize(Roles = …)]
        // here and on the API agree by construction.
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // The bearer token rides in the cookie, which is HttpOnly. The workspace shell is the
        // one place that renders it for the front end, which calls the API directly.
        if (result.AccessToken is not null) claims.Add(new Claim(ApiTokenClaim, result.AccessToken));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                // Capped to the token's own life. A cookie outliving its bearer token leaves
                // the app looking signed in while every API call returns 401.
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
            });

        return SafeRedirect(model.ReturnUrl, user.Roles);
    }

    /// <summary>
    /// Signs the user out.
    /// <para>
    /// Deliberately anonymous. The [Authorize] that used to sit here did nothing — a
    /// controller-level [AllowAnonymous] wins over an action-level [Authorize], which the
    /// ASP0026 analyser flags — so it only looked protective. Requiring authentication to sign
    /// out is also the wrong shape: a caller whose cookie has already expired would be refused
    /// the very thing that clears it. The antiforgery token is what matters here, because it
    /// is what stops another site logging the user out on their behalf.
    /// </para>
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Denied()
    {
        ViewData["Title"] = "Access denied";
        return View();
    }

    /// <summary>
    /// Decides where a freshly signed-in user lands.
    /// <para>
    /// A return URL wins over the role's landing page. Someone who followed a link and was
    /// bounced to sign in expects to arrive where they were going, and overriding that would
    /// break every deep link into the app for administrators in particular.
    /// </para>
    /// <para>
    /// Only a LOCAL return URL is followed. An absolute one would let a crafted link bounce a
    /// freshly signed-in user to another site — the classic open redirect.
    /// </para>
    /// </summary>
    private IActionResult SafeRedirect(string? returnUrl, IEnumerable<string> roles)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        // Case-insensitive: the role name travels as a string from the API, and a casing
        // difference should not silently send an administrator to the wrong page.
        if (roles.Contains(SuperAdminRole, StringComparer.OrdinalIgnoreCase))
            return RedirectToAction("Workspace", "It");

        return RedirectToAction("Index", "Home");
    }
}
