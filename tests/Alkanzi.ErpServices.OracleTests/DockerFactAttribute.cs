namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// A <see cref="FactAttribute"/> that skips instead of failing unless an Oracle
/// connection is configured through <see cref="OracleConnectionSource"/>.
/// </summary>
/// <remarks>
/// No container fallback, unlike the EF Core package's Oracle tests: these run
/// against the real ERP's own tables and data, which a throwaway container does
/// not have. A machine with no ERP connection has verified nothing here, so the
/// honest result is a skip.
/// </remarks>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (OracleConnectionSource.IsConfigured)
        {
            return;
        }

        Skip = SkipReason;
    }

    /// <summary>The reason a live-ERP test skips when no connection is configured.</summary>
    internal static string SkipReason =>
        "No ERP connection configured — supply one via " +
        $"`dotnet user-secrets set \"{OracleConnectionSource.SecretKey}\" \"...\"` " +
        $"or the {OracleConnectionSource.EnvironmentVariable} environment variable.";
}

/// <summary>
/// A <see cref="TheoryAttribute"/> that skips instead of failing unless an Oracle
/// connection is configured — the data-driven counterpart of
/// <see cref="DockerFactAttribute"/>, for covering several document types with one
/// test body.
/// </summary>
public sealed class DockerTheoryAttribute : TheoryAttribute
{
    public DockerTheoryAttribute()
    {
        if (OracleConnectionSource.IsConfigured)
        {
            return;
        }

        Skip = DockerFactAttribute.SkipReason;
    }
}
