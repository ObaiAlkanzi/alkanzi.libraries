namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Looks up document-type configuration for the current tenant and loads the
/// transaction rows it points at, without the caller naming a concrete
/// transaction type.
/// </summary>
/// <typeparam name="TMenu">
/// Your document-type registry entity, mapped in the same
/// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
/// </typeparam>
public interface IApprovalEngine<TMenu>
    where TMenu : class, ITransactionMenu
{
    /// <summary>
    /// Returns the registry row for a document type in the tenant supplied by
    /// <see cref="ICompanyContext"/>, or <see langword="null"/> if none is
    /// configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// More than one row matches, meaning the registry holds duplicates for one
    /// document type and tenant.
    /// </exception>
    Task<TMenu?> GetMenuAsync(string docType, CancellationToken cancellationToken = default);

    /// <summary>
    /// As <see cref="GetMenuAsync(string, CancellationToken)"/>, but for an
    /// explicit tenant rather than the ambient one — for jobs that span
    /// companies.
    /// </summary>
    /// <param name="docType">Document type code.</param>
    /// <param name="orgId">Organisation id.</param>
    /// <param name="compId">Company id.</param>
    /// <param name="branchId">Branch id, or <see langword="null"/> to leave the lookup unscoped by branch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TMenu?> GetMenuAsync(string docType, int orgId, int compId, int? branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a transaction row by table name and primary key. Returns
    /// <see langword="null"/> when the row does not exist or is soft-deleted.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No entity type is mapped to <paramref name="tableName"/>.
    /// </exception>
    ValueTask<object?> GetTransactionAsync(string tableName, object transId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a document type to its table via
    /// <see cref="ITransactionMenu.TABLE_NAME"/> and loads the transaction in
    /// one step, scoped to the ambient tenant.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The document type is not configured for this tenant, its table name is
    /// null or blank, or no entity type is mapped to that table.
    /// </exception>
    Task<object?> GetTransactionByDocTypeAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>
    /// As <see cref="GetTransactionByDocTypeAsync"/>, but typed: returns the row
    /// as <see cref="IApprovable"/> so callers can read approval state without
    /// knowing the concrete entity. Returns <see langword="null"/> when the row
    /// does not exist or is soft-deleted.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Everything <see cref="GetTransactionByDocTypeAsync"/> throws for, plus the
    /// resolved entity not implementing <see cref="IApprovable"/> — a handful of
    /// dispatchable tables carry no approval columns.
    /// </exception>
    Task<IApprovable?> GetApprovableByDocTypeAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a transaction's approval state on, and saves.
    /// </summary>
    /// <remarks>
    /// <see cref="IApprovable.APPROVE_STATUS"/> always becomes the action's own
    /// code. What happens to <see cref="IApprovable.APPROVE_LEVEL"/> depends on
    /// the action:
    /// <list type="table">
    /// <item><term><see cref="ApprovalAction.Submit"/></term><description>climbs one level</description></item>
    /// <item><term><see cref="ApprovalAction.Rework"/></term><description>drops to <paramref name="targetLevel"/></description></item>
    /// <item><term><see cref="ApprovalAction.Reject"/></term><description>unchanged — it records the level that refused</description></item>
    /// <item><term><see cref="ApprovalAction.Approve"/></term><description>climbs one level</description></item>
    /// </list>
    /// <para>
    /// Saving is <c>SaveChangesAsync</c> on the whole context, so any other
    /// pending changes it is tracking are committed in the same transaction. If
    /// the entity is also <see cref="IAuditable"/> the interceptor stamps
    /// <c>UPDATED_BY</c> and <c>UPDATED_AT</c> as part of that save.
    /// </para>
    /// </remarks>
    /// <param name="docType">Document type code, resolved through the registry.</param>
    /// <param name="transId">Primary key of the transaction row.</param>
    /// <param name="action">The transition to perform.</param>
    /// <param name="targetLevel">
    /// Level to drop to. Meaningful only for <see cref="ApprovalAction.Rework"/>;
    /// passing a non-zero value with any other action is an error rather than
    /// being quietly ignored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated row.</returns>
    /// <exception cref="InvalidOperationException">
    /// Everything <see cref="GetApprovableByDocTypeAsync"/> throws for, plus the
    /// row not existing or being soft-deleted — a transition against a row that
    /// is not there is a mistake, not a no-op.
    /// </exception>
    Task<IApprovable> ApplyApprovalAsync(
        string docType,
        object transId,
        ApprovalAction action,
        int targetLevel = 0,
        CancellationToken cancellationToken = default);
}
