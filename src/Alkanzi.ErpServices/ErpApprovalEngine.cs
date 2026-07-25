using Microsoft.EntityFrameworkCore;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
/// <remarks>
/// Dispatch resolves a document type to a table through the registry, then a
/// table name to a mapped CLR type through EF's model — so a new transaction
/// table needs only to be mapped on <see cref="ErpDbContext"/>, not named here.
/// Saving runs through whatever interceptor the context carries, so approvals
/// are audit-stamped without this engine knowing about auditing.
/// </remarks>
public sealed class ErpApprovalEngine : IErpApprovalEngine
{
    private readonly ErpDbContext _context;
    private readonly IErpCompanyContext _company;

    /// <summary>Creates the engine over the ERP context and the current tenant.</summary>
    public ErpApprovalEngine(ErpDbContext context, IErpCompanyContext company)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _company = company ?? throw new ArgumentNullException(nameof(company));
    }

    /// <inheritdoc />
    public async Task<IErpTransactionMenu?> GetMenuAsync(string docType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(docType);

        // The concrete registry entity is queried directly, so its columns
        // translate without EF.Property indirection. Scoped by tenant because a
        // document type is registered once per company and branch, and only the
        // four columns together identify a single configuration.
        var query = _context.TransactionMenus.Where(m =>
            m.DOC_TYPE == docType
            && m.ORG_ID == _company.ORG_ID
            && m.COMP_ID == _company.COMP_ID);

        if (_company.BRANCH_ID is not null)
        {
            query = query.Where(m => m.BRANCH_ID == _company.BRANCH_ID);
        }

        // SingleOrDefault, not First: a second match means the registry holds
        // duplicate configuration for one tenant, which should surface.
        return await query.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<object?> GetTransactionAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transId);

        var menu = await GetMenuAsync(docType, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No {nameof(FM_TRANSACTION_MENU)} row is configured for document type '{docType}' " +
                $"in organisation {_company.ORG_ID}, company {_company.COMP_ID}, branch {_company.BRANCH_ID?.ToString() ?? "(any)"}.");

        if (string.IsNullOrWhiteSpace(menu.TABLE_NAME))
        {
            throw new InvalidOperationException(
                $"Document type '{docType}' has no TABLE_NAME, so its transaction table is unknown. " +
                "Most registry rows configure numbering and workflow only, and cannot be dispatched to a transaction.");
        }

        var clrType = ResolveClrType(menu.TABLE_NAME)
            ?? throw new InvalidOperationException(
                $"No entity type is mapped to table '{menu.TABLE_NAME}'. " +
                $"Map it on {nameof(ErpDbContext)} for document type '{docType}' to dispatch.");

        var entity = await _context.FindAsync(clrType, [transId], cancellationToken).ConfigureAwait(false);

        // The query filter already excludes soft-deleted rows from the SELECT
        // Find issues. It does not help when the row is already tracked, because
        // Find then returns it from the change tracker without querying.
        return entity is IErpAuditable { IS_DELETED: true } ? null : entity;
    }

    /// <inheritdoc />
    public async Task<IErpApprovable?> GetAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        var row = await GetTransactionAsync(docType, transId, cancellationToken).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        return row as IErpApprovable
            ?? throw new InvalidOperationException(
                $"Document type '{docType}' resolves to {row.GetType().Name}, which does not implement " +
                $"{nameof(IErpApprovable)}. That table carries no approval columns, so it cannot be approved.");
    }

    /// <inheritdoc />
    public Task<IErpApprovable> SubmitAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Submit, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<IErpApprovable> ApproveAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Approve, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<IErpApprovable> RejectAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Reject, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<IErpApprovable> ReworkAsync(string docType, object transId, int targetLevel, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Rework, targetLevel, cancellationToken);

    /// <inheritdoc />
    public async Task<IErpApprovable> ApplyApprovalAsync(
        string docType,
        object transId,
        ApprovalAction action,
        int targetLevel = 0,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(
                nameof(action),
                action,
                $"Not an approval action. Expected one of: {string.Join(", ", Enum.GetNames<ApprovalAction>())}.");
        }

        if (action is not ApprovalAction.Rework && targetLevel != 0)
        {
            throw new ArgumentException(
                $"{nameof(targetLevel)} applies only to {nameof(ApprovalAction.Rework)}, " +
                $"but {action} was requested with level {targetLevel}.",
                nameof(targetLevel));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(targetLevel);

        var row = await GetAsync(docType, transId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No row with key '{transId}' exists for document type '{docType}', or it is soft-deleted. " +
                $"There is nothing to {action.ToString().ToLowerInvariant()}.");

        // The status is the action's own code by construction.
        row.APPROVE_STATUS = (int)action;

        row.APPROVE_LEVEL = action switch
        {
            ApprovalAction.Submit or ApprovalAction.Approve => row.APPROVE_LEVEL + 1,
            ApprovalAction.Rework => targetLevel,

            // Reject leaves the level alone, recording which level refused it.
            _ => row.APPROVE_LEVEL,
        };

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return row;
    }

    /// <summary>
    /// Maps a table name to the CLR type mapped to it, through EF's model. Owned
    /// types and TPH subclasses share a table with their owner/root, so they are
    /// skipped to avoid a false match.
    /// </summary>
    private Type? ResolveClrType(string tableName)
    {
        var target = tableName.Trim();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            if (string.Equals(entityType.GetTableName(), target, StringComparison.OrdinalIgnoreCase))
            {
                return entityType.ClrType;
            }
        }

        return null;
    }
}
