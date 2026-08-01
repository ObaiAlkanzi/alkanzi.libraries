using Microsoft.Extensions.Configuration;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Resolves the Oracle connection string for the ERP service tests, with optional
/// selection between several named databases (e.g. <c>dev</c> and <c>live</c>).
/// </summary>
/// <remarks>
/// <para>
/// Default (no target chosen) is unchanged: the <see cref="EnvironmentVariable"/>
/// first, then user secrets under <see cref="SecretKey"/> — the same keys and
/// <c>UserSecretsId</c> as the EF Core package's Oracle tests, so one connection
/// serves both projects. Tests <b>skip</b> when nothing is configured.
/// </para>
/// <para>
/// To keep more than one database and switch between them, set a target name via
/// <c>ALKANZI_ORACLE_TARGET</c> (or the <c>Oracle:Target</c> secret) and store each
/// connection under a named key. For <c>ALKANZI_ORACLE_TARGET=live</c> the string
/// is read from the <c>ALKANZI_ORACLE_CONNECTION_LIVE</c> environment variable, or
/// the <c>Oracle:Connections:Live</c> user secret. Store secrets, never commit:
/// <code>
/// dotnet user-secrets set "Oracle:Connections:Live" "&lt;connection string&gt;" \
///   --project tests/Alkanzi.ErpServices.OracleTests
/// # then run against it:
/// $env:ALKANZI_ORACLE_TARGET = "live";  dotnet test tests/Alkanzi.ErpServices.OracleTests
/// </code>
/// If the named connection is missing, it falls back to the default one above.
/// </para>
/// </remarks>
internal static class OracleConnectionSource
{
    public const string EnvironmentVariable = "ALKANZI_ORACLE_CONNECTION";
    public const string SecretKey = "Oracle:ConnectionString";

    /// <summary>Selects a named connection (e.g. <c>dev</c>, <c>live</c>).</summary>
    public const string TargetEnvironmentVariable = "ALKANZI_ORACLE_TARGET";
    public const string TargetSecretKey = "Oracle:Target";

    private static readonly Lazy<Resolution> Lazy = new(Resolve, isThreadSafe: true);

    public static string? ConnectionString => Lazy.Value.ConnectionString;

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>The chosen target name, or <c>null</c> when the default connection is used.</summary>
    public static string? Target => Lazy.Value.Target;

    private sealed record Resolution(string? ConnectionString, string? Target);

    private static Resolution Resolve()
    {
        IConfiguration configuration;
        try
        {
            configuration = new ConfigurationBuilder()
                .AddUserSecrets(typeof(OracleConnectionSource).Assembly, optional: true)
                .Build();
        }
        catch
        {
            configuration = new ConfigurationBuilder().Build();
        }

        // A named target picks a specific database; try it first, then fall back.
        var target = Environment.GetEnvironmentVariable(TargetEnvironmentVariable)
            ?? configuration[TargetSecretKey];
        target = string.IsNullOrWhiteSpace(target) ? null : target.Trim();

        if (target is not null)
        {
            var named = Environment.GetEnvironmentVariable($"{EnvironmentVariable}_{target.ToUpperInvariant()}");
            if (string.IsNullOrWhiteSpace(named))
            {
                named = configuration[$"Oracle:Connections:{target}"];
            }

            if (!string.IsNullOrWhiteSpace(named))
            {
                return new Resolution(named, target);
            }
        }

        // Default connection (unchanged behaviour).
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return new Resolution(fromEnvironment, target);
        }

        var fromSecrets = configuration[SecretKey];
        return new Resolution(string.IsNullOrWhiteSpace(fromSecrets) ? null : fromSecrets, target);
    }
}
