namespace Alkanzi.Erp.Infrastructure;

/// <summary>
/// Authenticates a user against Alkanzi.Erp.Api.
/// <para>
/// The web project holds no database connection, so it cannot use Identity's SignInManager to
/// check a password. It posts the credentials to the API instead and turns the response into
/// its own sign-in cookie. That keeps one door to the data: the API decides who a user is and
/// what they may do, and the web app renders the result.
/// </para>
/// <para>
/// Server-side rather than from the browser, so the password never travels to a second origin
/// and the API being down is a message on the login page rather than a silent failure.
/// </para>
/// </summary>
public sealed class ApiAuthClient
{
    public const string HttpClientName = "ErpApi";

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ApiAuthClient> _logger;

    public ApiAuthClient(IHttpClientFactory factory, ILogger<ApiAuthClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public sealed record AuthenticatedUser(
        int Id, string FullName, string Email, int CompanyId, int OrganizationId, int? BranchId, string[] Roles);

    public sealed record AuthResult(
        bool Succeeded, string? AccessToken, AuthenticatedUser? User, string? Error, bool Unreachable = false);

    private sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, AuthenticatedUser User);
    private sealed record ErrorResponse(string? Error, string? Message);

    public async Task<AuthResult> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var client = _factory.CreateClient(HttpClientName);
            var response = await client.PostAsJsonAsync("api/auth/token", new { email, password }, ct);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
                return payload is null
                    ? new AuthResult(false, null, null, "The API returned an unreadable response.")
                    : new AuthResult(true, payload.AccessToken, payload.User, null);
            }

            // The API deliberately returns one message for unknown user, wrong password and
            // disabled account — pass its wording through rather than inventing our own, so
            // the two front doors cannot disagree about what happened.
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return new AuthResult(false, null, null, error?.Message ?? "Sign-in failed.");
        }
        catch (Exception ex)
        {
            // Distinguished from a rejected password: telling a user their credentials are
            // wrong when the API is simply down sends them chasing the wrong problem.
            _logger.LogError(ex, "Could not reach the API to authenticate {Email}.", email);
            return new AuthResult(false, null, null,
                "Cannot reach the ERP service. Try again shortly.", Unreachable: true);
        }
    }
}
