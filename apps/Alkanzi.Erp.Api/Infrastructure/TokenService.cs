using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alkanzi.Erp.Domain.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Alkanzi.Erp.Api.Infrastructure;

/// <summary>Mints signed access tokens for authenticated users.</summary>
public sealed class TokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            // NameIdentifier, company and branch use the same claim types the MVC app writes
            // into its cookie, so HttpCurrentUser reads either credential unchanged.
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(HttpCurrentUser.CompanyIdClaim, user.CompanyId.ToString()),
            new(HttpCurrentUser.BranchIdClaim, user.BranchId?.ToString() ?? ""),
            new("erp:full_name", user.FullName),

            // A unique token id, so a future revocation list has something to key on.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
