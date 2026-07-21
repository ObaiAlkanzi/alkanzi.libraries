using Microsoft.Extensions.Configuration;

namespace Alkanzi.Auditable.EntityFrameworkCore.OracleTests;

/// <summary>
/// Resolves the Oracle connection string for the integration tests, so the
/// fixture and <see cref="DockerFactAttribute"/> always agree on whether an
/// instance is configured.
/// </summary>
/// <remarks>
/// Two sources, in order: the <see cref="EnvironmentVariable"/> (handy in CI),
/// then .NET user secrets under <see cref="SecretKey"/> (handy locally — the
/// value lives in your user profile, never in the repo, and is picked up by
/// IDE test runners as well as the CLI).
/// </remarks>
internal static class OracleConnectionSource
{
    public const string EnvironmentVariable = "ALKANZI_ORACLE_CONNECTION";
    public const string SecretKey = "Oracle:ConnectionString";

    // Resolved once: called per test at discovery, and re-reading the secrets
    // file each time would be wasteful.
    private static readonly Lazy<string?> Lazy = new(Resolve, isThreadSafe: true);

    public static string? ConnectionString => Lazy.Value;

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    private static string? Resolve()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(OracleConnectionSource).Assembly, optional: true)
                .Build();

            var fromSecrets = configuration[SecretKey];
            return string.IsNullOrWhiteSpace(fromSecrets) ? null : fromSecrets;
        }
        catch
        {
            // No secrets configured on this machine — same as not set.
            return null;
        }
    }
}
