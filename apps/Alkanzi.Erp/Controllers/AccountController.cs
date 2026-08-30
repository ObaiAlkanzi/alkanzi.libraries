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

    public const string CompanyIdClaim = "erp:company_id";
    public const string OrganizationIdClaim = "erp:organization_id";
    public const string BranchIdClaim = "erp:branch_id";
    public const string FullNameClaim = "erp:full_name";

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
            new(CompanyIdClaim, user.CompanyId.ToString()),
            new(OrganizationIdClaim, user.OrganizationId.ToString()),
            new(BranchIdClaim, user.BranchId?.ToString() ?? ""),
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

        return SafeRedirect(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
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
    /// Only follows a local return URL. An absolute one would let a crafted link bounce a
    /// freshly signed-in user to another site — the classic open redirect.
    /// </summary>
    private IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
