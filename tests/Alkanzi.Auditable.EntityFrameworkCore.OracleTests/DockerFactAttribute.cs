using System.Diagnostics;

namespace Alkanzi.Auditable.EntityFrameworkCore.OracleTests;

/// <summary>
/// A <see cref="FactAttribute"/> that skips instead of failing when no Oracle
/// instance can be reached — neither a container nor one supplied through
/// <see cref="OracleFixture.ConnectionStringVariable"/> — so <c>dotnet test</c>
/// stays green on machines that cannot run containers.
/// </summary>
/// <remarks>
/// Skipping is the honest outcome here: a machine with no Oracle available has
/// not verified this behaviour, and the result should say so rather than
/// report a pass.
/// </remarks>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (OracleConnectionSource.IsConfigured || DockerProbe.IsAvailable)
        {
            return;
        }

        Skip = "No Oracle available — start Docker, or supply a connection string via " +
               $"`dotnet user-secrets set \"{OracleConnectionSource.SecretKey}\" \"...\"` " +
               $"or the {OracleConnectionSource.EnvironmentVariable} environment variable.";
    }
}

internal static class DockerProbe
{
    private static readonly Lazy<bool> Probe = new(Detect, isThreadSafe: true);

    public static bool IsAvailable => Probe.Value;

    /// <summary>
    /// Shells out to the Docker CLI rather than binding Docker.DotNet directly:
    /// that package only arrives transitively through Testcontainers, and
    /// compiling against a transitive dependency breaks on its next bump.
    /// </summary>
    private static bool Detect()
    {
        // Explicit override for CI images where the CLI is absent but the
        // daemon endpoint is configured through DOCKER_HOST.
        if (Environment.GetEnvironmentVariable("ALKANZI_FORCE_ORACLE_TESTS") == "1")
        {
            return true;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info --format {{.OSType}}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            // A daemon that is installed but not started hangs rather than
            // erroring, so the probe must not wait indefinitely.
            if (!process.WaitForExit(15_000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Nothing useful to do — treated as unavailable either way.
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
