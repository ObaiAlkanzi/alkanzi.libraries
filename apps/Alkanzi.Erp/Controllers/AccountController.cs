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
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        ILogger<AccountController> logger)
    {
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
        await _signIn.SignInWithClaimsAsync(user, model.RememberMe, new[]
        {
            new Claim(HttpCurrentUser.CompanyIdClaim, user.CompanyId.ToString()),
            new Claim(HttpCurrentUser.BranchIdClaim, user.BranchId?.ToString() ?? ""),
            new Claim("erp:full_name", user.FullName),
        });

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
