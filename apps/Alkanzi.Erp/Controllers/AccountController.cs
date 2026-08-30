using System.Security.Claims;
using Alkanzi.Erp.Domain.Security;
using Alkanzi.Erp.Infrastructure;
using Alkanzi.Erp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    /// <summary>Claim carrying the API bearer token issued alongside the sign-in cookie.</summary>
    public const string ApiTokenClaim = "erp:api_token";

    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApiTokenClient _apiTokens;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        ApiTokenClient apiTokens,
        ILogger<AccountController> logger)
    {
        _apiTokens = apiTokens;
        _signIn = signIn;
        _users = users;
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

        var user = await _users.FindByEmailAsync(model.Email);

        // One message for "no such user" and for "wrong password", on purpose: distinguishing
        // them tells an attacker which addresses are real accounts.
        const string failed = "Incorrect email or password.";

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, failed);
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been disabled.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login blocked: {Email} is locked out.", model.Email);
            ModelState.AddModelError(string.Empty, "Too many failed attempts. Try again later.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, failed);
            return View(model);
        }

        // Company and branch are stamped into the cookie so ICurrentUser can answer without a
        // query on every request. They change rarely; a user moved between companies has to
        // sign in again for it to take effect.
        // Exchange the same credentials for an API token while they are still in hand — this
        // is the only moment the password exists in memory, and the front end needs a bearer
        // token to call the API directly.
        var apiToken = await _apiTokens.RequestTokenAsync(model.Email, model.Password);

        var claims = new List<Claim>
        {
            new(HttpCurrentUser.CompanyIdClaim, user.CompanyId.ToString()),
            new(HttpCurrentUser.BranchIdClaim, user.BranchId?.ToString() ?? ""),
            new("erp:full_name", user.FullName),
        };

        // Carried in the auth cookie, which is HttpOnly, so the token is not readable by
        // script until the shell deliberately renders it for the API client.
        if (apiToken is not null) claims.Add(new Claim(ApiTokenClaim, apiToken));

        await _signIn.SignInWithClaimsAsync(user, model.RememberMe, claims);

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return SafeRedirect(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Denied()
    {
        ViewData["Title"] = "Access denied";
        return View();
    }

    /// <summary>
    /// Only follows a return URL that is local. An absolute URL here would let a crafted link
    /// bounce a freshly signed-in user to another site — the classic open-redirect.
    /// </summary>
    private IActionResult SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
