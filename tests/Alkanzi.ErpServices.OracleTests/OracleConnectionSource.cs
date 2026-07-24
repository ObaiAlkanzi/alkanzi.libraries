using Microsoft.Extensions.Configuration;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Resolves the Oracle connection string for the ERP service tests.
/// </summary>
/// <remarks>
/// Same two sources, and the same keys, as the EF Core package's Oracle tests —
/// the <see cref="EnvironmentVariable"/> first, then user secrets under
/// <see cref="SecretKey"/>. The <c>UserSecretsId</c> in the csproj matches too,
/// so a connection configured once serves both test projects.
/// </remarks>
internal static class OracleConnectionSource
{
    public const string EnvironmentVariable = "ALKANZI_ORACLE_CONNECTION";
    public const string SecretKey = "Oracle:ConnectionString";

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
            return null;
        }
    }
}
