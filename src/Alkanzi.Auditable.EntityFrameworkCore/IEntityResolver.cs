using Microsoft.EntityFrameworkCore.Metadata;

namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Resolves a database table name to the entity type mapped to it, and loads
/// rows by primary key without the caller knowing the CLR type.
/// </summary>
/// <remarks>
/// For dispatching on a table name held in data — a workflow row that names the
/// table its transaction lives in, for example. Everything is driven by EF's
/// model, so no table-to-type registry has to be maintained by hand.
/// </remarks>
public interface IEntityResolver
{
    /// <summary>
    /// Returns the entity type mapped to <paramref name="tableName"/>, or
    /// <see langword="null"/> if the model maps nothing to it. Matching is
    /// case-insensitive and accepts either <c>TABLE</c> or <c>SCHEMA.TABLE</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// More than one entity type is mapped to the table and the intended one is
    /// ambiguous.
    /// </exception>
    IEntityType? FindEntityType(string tableName);

    /// <summary>
    /// As <see cref="FindEntityType"/>, but throws when nothing is mapped.
    /// </summary>
    /// <exception cref="InvalidOperationException">No entity maps to the table.</exception>
    IEntityType GetEntityType(string tableName);

    /// <summary>
    /// Loads the row with the given primary key from <paramref name="tableName"/>.
    /// Returns <see langword="null"/> when no such row exists, or when the row is
    /// soft-deleted.
    /// </summary>
    /// <remarks>
    /// Soft-deleted rows are excluded, matching
    /// <see cref="ModelBuilderExtensions.ApplyAuditableQueryFilters"/>. Mostly
    /// the query filter does this by itself — EF's <c>Find</c> applies filters
    /// on the query it issues — but not when the entity is already tracked, in
    /// which case <c>Find</c> returns it straight from the change tracker
    /// without querying. The explicit check covers that case. Use
    /// <see cref="FindIncludingDeletedAsync(string, object, CancellationToken)"/>
    /// when you want the row regardless.
    /// </remarks>
    ValueTask<object?> FindAsync(string tableName, object keyValue, CancellationToken cancellationToken = default);

    /// <summary>Composite-key overload of <see cref="FindAsync(string, object, CancellationToken)"/>.</summary>
    ValueTask<object?> FindAsync(string tableName, object[] keyValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a row by primary key, returning it even if it is soft-deleted.
    /// </summary>
    ValueTask<object?> FindIncludingDeletedAsync(string tableName, object keyValue, CancellationToken cancellationToken = default);

    /// <summary>Composite-key overload of <see cref="FindIncludingDeletedAsync(string, object, CancellationToken)"/>.</summary>
    ValueTask<object?> FindIncludingDeletedAsync(string tableName, object[] keyValues, CancellationToken cancellationToken = default);
}
