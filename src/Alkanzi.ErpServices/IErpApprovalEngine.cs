namespace Alkanzi.ErpServices;

/// <summary>
/// Looks up document-type configuration for the current tenant, loads the
/// transaction rows it points at, and moves their approval state on — the ERP's
/// own engine, owing nothing to an external library.
/// </summary>
public interface IErpApprovalEngine
{
    /// <summary>
    /// Returns the registry row for a document type in the current tenant, or
    /// <see langword="null"/> if none is configured.
    /// </summary>
    Task<IErpTransactionMenu?> GetMenuAsync(string docType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a document type to its table and loads the transaction row.
    /// Returns <see langword="null"/> when the row does not exist or is
    /// soft-deleted.
    /// </summary>
    Task<object?> GetTransactionAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>
    /// As <see cref="GetTransactionAsync"/>, but typed as
    /// <see cref="IErpApprovable"/>.
    /// </summary>
    Task<IErpApprovable?> GetAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Sends a document up the chain: climbs one level, status becomes submitted.</summary>
    Task<IErpApprovable> SubmitAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Passes a document: climbs one level, status becomes approved.</summary>
    Task<IErpApprovable> ApproveAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Refuses a document: status becomes rejected, level records where it stopped.</summary>
    Task<IErpApprovable> RejectAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Sends a document back for correction: status becomes rework, level drops to <paramref name="targetLevel"/>.</summary>
    Task<IErpApprovable> ReworkAsync(string docType, object transId, int targetLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// The general form behind the verbs: applies <paramref name="action"/> to
    /// the transaction and saves. Only <see cref="ApprovalAction.Rework"/> uses
    /// <paramref name="targetLevel"/>.
    /// </summary>
    Task<IErpApprovable> ApplyApprovalAsync(
        string docType,
        object transId,
        ApprovalAction action,
        int targetLevel = 0,
        CancellationToken cancellationToken = default);
}
