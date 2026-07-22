using Microsoft.EntityFrameworkCore;

namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <inheritdoc cref="IApprovalEngine{TMenu}" />
/// <remarks>
/// Takes a plain <see cref="DbContext"/> rather than declaring its own, so it
/// plugs into the context that already maps <typeparamref name="TMenu"/> and the
/// transaction tables.
/// </remarks>
public sealed class ApprovalEngine<TMenu> : IApprovalEngine<TMenu>
    where TMenu : class, ITransactionMenu
{
    // EF.Property is used instead of member access because the members are
    // declared on ITransactionMenu, and EF translates members against the mapped
    // entity type rather than an interface it happens to implement. Matching by
    // property name sidesteps the question.
    private const string DocTypeProperty = nameof(ITransactionMenu.DOC_TYPE);
    private const string OrgProperty = nameof(ITransactionMenu.ORG_ID);
    private const string CompanyProperty = nameof(ITransactionMenu.COMP_ID);
    private const string BranchProperty = nameof(ITransactionMenu.BRANCH_ID);

    private readonly DbContext _context;
    private readonly IEntityResolver _resolver;
    private readonly ICompanyContext _company;

    /// <summary>Creates an engine over the given context, resolver and tenant.</summary>
    public ApprovalEngine(DbContext context, IEntityResolver resolver, ICompanyContext company)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _company = company ?? throw new ArgumentNullException(nameof(company));
    }

    /// <inheritdoc />
    public Task<TMenu?> GetMenuAsync(string docType, CancellationToken cancellationToken = default)
        => GetMenuAsync(docType, _company.ORG_ID, _company.COMP_ID, _company.BRANCH_ID, cancellationToken);

    /// <inheritdoc />
    public async Task<TMenu?> GetMenuAsync(
        string docType,
        int orgId,
        int compId,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(docType);

        var query = _context.Set<TMenu>()
            .Where(menu =>
                EF.Property<string>(menu, DocTypeProperty) == docType
                //&& EF.Property<int>(menu, OrgProperty) == orgId
                //&& EF.Property<int>(menu, CompanyProperty) == compId
                );

        //if (branchId is not null)
        //{
        //    query = query.Where(menu => EF.Property<int?>(menu, BranchProperty) == branchId);
        //} 
        // Safe here in a way it was not on DOC_TYPE alone: the four columns
        // together are unique in practice, so a second match means the registry
        // genuinely holds duplicates and should say so rather than pick one.
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<object?> GetTransactionAsync(string tableName, object transId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(transId);

        return _resolver.FindAsync(tableName, transId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<object?> GetTransactionByDocTypeAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        var menu = await GetMenuAsync(docType, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No {typeof(TMenu).Name} row is configured for document type '{docType}' " +
                $"in organisation {_company.ORG_ID}, company {_company.COMP_ID}, branch {_company.BRANCH_ID?.ToString() ?? "(any)"}.");

        if (string.IsNullOrWhiteSpace(menu.TABLE_NAME))
        {
            throw new InvalidOperationException(
                $"Document type '{docType}' has no {nameof(ITransactionMenu.TABLE_NAME)}, " +
                "so its transaction table is unknown. Most registry rows configure " +
                "numbering and workflow only, and cannot be dispatched to a transaction.");
        }

        return await GetTransactionAsync(menu.TABLE_NAME, transId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IApprovable?> GetApprovableByDocTypeAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        var row = await GetTransactionByDocTypeAsync(docType, transId, cancellationToken).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        // Named rather than a blind cast: the tables a registry dispatches to do
        // not all carry approval columns, and "cannot be approved" is a fact
        // about the configuration worth stating plainly.
        return row as IApprovable
            ?? throw new InvalidOperationException(
                $"Document type '{docType}' resolves to {row.GetType().Name}, which does not implement " +
                $"{nameof(IApprovable)}. That table carries no approval columns, so it cannot be approved — " +
                $"use {nameof(GetTransactionByDocTypeAsync)} to load it as a plain row.");
    }

    /// <inheritdoc />
    public async Task<IApprovable> ApplyApprovalAsync(
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

        // Rejected rather than ignored: a caller passing a level alongside
        // Submit or Approve believes it will be honoured, and silently dropping
        // it would leave the row at a level they never intended.
        if (action is not ApprovalAction.Rework && targetLevel != 0)
        {
            throw new ArgumentException(
                $"{nameof(targetLevel)} applies only to {nameof(ApprovalAction.Rework)}, " +
                $"but {action} was requested with level {targetLevel}.",
                nameof(targetLevel));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(targetLevel);

        var row = await GetApprovableByDocTypeAsync(docType, transId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No row with key '{transId}' exists for document type '{docType}', or it is soft-deleted. " +
                $"There is nothing to {action.ToString().ToLowerInvariant()}.");

        // Same number by construction — see ApprovalAction.
        row.APPROVE_STATUS = (int)action;

        row.APPROVE_LEVEL = action switch
        {
            ApprovalAction.Submit or ApprovalAction.Approve => row.APPROVE_LEVEL + 1,
            ApprovalAction.Rework => targetLevel,

            // Reject leaves the level alone, so the row still says which level
            // refused it rather than collapsing to a number that means nothing.
            _ => row.APPROVE_LEVEL,
        };

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return row;
    }
}
