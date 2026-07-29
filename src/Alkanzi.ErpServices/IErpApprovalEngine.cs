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

    /// <summary>
    /// Returns the workflows configured for a document type, straight from
    /// <c>APPROVAL_REVERT_PAK.GET_TRANS_WF</c> — one entry per <c>WF_ID</c>, each
    /// carrying the <c>MAP_FUN</c> that disambiguates when there is more than one.
    /// </summary>
    Task<IReadOnlyList<TransactionWorkflow>> GetWorkflowsAsync(string docType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the single workflow that applies to one transaction: the sole
    /// configured workflow, or — when several are configured — the one the
    /// cursor's <c>MAP_FUN</c> selects for this <paramref name="transId"/>. Loads
    /// the form and its levels. Returns <see langword="null"/> when no workflow is
    /// configured or the resolved id maps to no form.
    /// </summary>
    Task<ResolvedWorkflow?> ResolveWorkflowAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Sends a document up the chain: climbs one level, status becomes submitted.</summary>
    Task<ApprovalResult> SubmitAsync(string docType, object transId, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default);

    /// <summary>Passes a document: climbs one level, status becomes approved.</summary>
    Task<ApprovalResult> ApproveAsync(string docType, object transId, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default);

    /// <summary>Refuses a document: status becomes rejected, level records where it stopped.</summary>
    Task<ApprovalResult> RejectAsync(string docType, object transId, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default);

    /// <summary>Sends a document back for correction: status becomes rework, level drops to <paramref name="targetLevel"/>.</summary>
    Task<ApprovalResult> ReworkAsync(string docType, object transId, int targetLevel, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// The general form behind the verbs: applies <paramref name="action"/> to
    /// the transaction and saves. Only <see cref="ApprovalAction.Rework"/> uses
    /// <paramref name="targetLevel"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="ApprovalResult"/> describing the outcome. Ordinary answers —
    /// the row not existing, being already approved or rejected, or not being
    /// approvable — come back as results, not exceptions.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="action"/> is not a defined action, or <paramref name="targetLevel"/> is negative.</exception>
    /// <exception cref="ArgumentException"><paramref name="targetLevel"/> is set for an action other than <see cref="ApprovalAction.Rework"/>.</exception>
    /// <exception cref="InvalidOperationException">The document type is not configured, or resolves to a table not mapped on the context.</exception>
    /// <remarks>
    /// <paramref name="remarks"/> is recorded on the approval-log detail row for this
    /// action. Pass a non-zero <paramref name="sgId"/> (security group) to gate the
    /// action on <c>APPROVAL_REVERT_PAK.LVL_AUTHORIZATION</c>; a denial comes back as
    /// an <see cref="ApprovalOutcome.NotAuthorized"/> result.
    /// </remarks>
    Task<ApprovalResult> ApplyApprovalAsync(
        string docType,
        object transId,
        ApprovalAction action,
        int targetLevel = 0,
        string? remarks = null,
        int sgId = 0,
        CancellationToken cancellationToken = default);
}
