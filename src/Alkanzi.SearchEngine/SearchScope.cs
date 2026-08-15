namespace Alkanzi.SearchEngine;

/// <summary>
/// The caller's permission context. The engine uses it to decide which providers may
/// run and which hits the user is allowed to see. Never trust results without it.
/// </summary>
public sealed class SearchScope
{
    public int UserId { get; init; }
    public int DepartmentId { get; init; }
    public int SecurityGroup { get; init; }

    /// <summary>Branches the user may see. Null = all branches (e.g. admin).</summary>
    public IReadOnlyCollection<int>? AllowedBranches { get; init; }

    /// <summary>Entity types the user may search. Null = all types.</summary>
    public IReadOnlyCollection<string>? AllowedTypes { get; init; }

    /// <summary>Unrestricted scope — use only for trusted/admin callers or tests.</summary>
    public static SearchScope All { get; } = new();
}
