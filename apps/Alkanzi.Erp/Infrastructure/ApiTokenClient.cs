namespace Alkanzi.Erp.Infrastructure;

/// <summary>
/// Fetches an API access token on the user's behalf at sign-in.
/// <para>
/// The web app authenticates with its own cookie, but the AngularJS front end calls
/// Alkanzi.Erp.Api directly, and that API only accepts bearer tokens. So the sign-in exchanges
/// the same credentials for a token once, server-side, and hands it to the page.
/// </para>
/// <para>
/// Server-side rather than from the browser so the password is never posted to a second
/// origin, and so a failure to reach the API is a log entry instead of a broken page.
/// </para>
/// </summary>
public sealed class ApiTokenClient
{
    public const string HttpClientName = "ErpApi";

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ApiTokenClient> _logger;

    public ApiTokenClient(IHttpClientFactory factory, ILogger<ApiTokenClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    private sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);

    /// <summary>
    /// Returns an access token, or null when the API could not issue one.
    /// <para>
    /// Null is not fatal: the user is still signed in to the web app, and the pages that do
    /// not call the API keep working. The ones that do will report their own 401.
    /// </para>
    /// </summary>
    public async Task<string?> RequestTokenAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var client = _factory.CreateClient(HttpClientName);

            var response = await client.PostAsJsonAsync("api/auth/token", new { email, password }, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API token request for {Email} failed with {Status}.", email, (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            return payload?.AccessToken;
        }
        catch (Exception ex)
        {
            // Most likely the API is not running. Worth a log, not worth failing the sign-in.
            _logger.LogWarning(ex, "Could not reach the API to issue a token for {Email}.", email);
            return null;
        }
    }
}
