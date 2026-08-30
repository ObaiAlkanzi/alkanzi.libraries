namespace Alkanzi.Erp.Api.Infrastructure;

/// <summary>
/// Token signing and validation settings, bound from configuration under "Jwt".
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Alkanzi.Erp";
    public string Audience { get; set; } = "Alkanzi.Erp.Api";

    /// <summary>
    /// Symmetric signing key. Never committed — it comes from user secrets in development and
    /// from the environment or a secret store elsewhere. Anyone holding it can mint a token
    /// for any user, so it is exactly as sensitive as the database password.
    /// </summary>
    public string SigningKey { get; set; } = "";

    /// <summary>
    /// Short by design. A bearer token cannot be revoked once issued — the only thing limiting
    /// the damage of a leaked one is how quickly it expires, so this is minutes rather than
    /// days, and clients refresh.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 30;
}
