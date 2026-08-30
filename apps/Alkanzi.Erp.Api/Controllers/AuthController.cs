using Alkanzi.Erp.Api.Infrastructure;
using Alkanzi.Erp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Alkanzi.Erp.Api.Controllers;

public sealed record TokenRequest(string Email, string Password);

/// <remarks>
/// No blanket [AllowAnonymous] on the class: it silently overrides an action-level
/// [Authorize] (ASP0026), which left /api/auth/me unauthenticated while looking protected.
/// Anonymous access is granted per action instead.
/// </remarks>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly TokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        TokenService tokens,
        ILogger<AuthController> logger)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Exchanges credentials for an access token.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromBody] TokenRequest request)
    {
        // One response for every failure mode — unknown user, wrong password, disabled
        // account. Distinguishing them tells an attacker which addresses are real accounts.
        var denied = Unauthorized(new { error = "invalid_credentials", message = "Incorrect email or password." });

        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive) return denied;

        // lockoutOnFailure so the API shares the MVC app's brute-force protection; without it
        // this endpoint would be an unthrottled way around the lockout policy.
        var result = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Token request blocked: {Email} is locked out.", request.Email);
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { error = "locked_out", message = "Too many failed attempts. Try again later." });
        }

        if (!result.Succeeded) return denied;

        var roles = await _users.GetRolesAsync(user);
        var (token, expiresAt) = _tokens.CreateAccessToken(user, roles);

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return Ok(new
        {
            accessToken = token,
            tokenType = "Bearer",
            expiresAtUtc = expiresAt,
            user = new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                companyId = user.CompanyId,
                branchId = user.BranchId,
                roles,
            }
        });
    }

    /// <summary>Echoes the caller's identity — useful for confirming a token is being read.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        name = User.Identity?.Name,
        claims = User.Claims.Select(c => new { c.Type, c.Value }),
    });
}
