using Alkanzi.Auditable.EntityFrameworkCore;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
/// <remarks>
/// Delegates to <see cref="IApprovalEngine{TMenu}"/> over
/// <see cref="FM_TRANSACTION_MENU"/>. It holds no state of its own — the engine
/// carries the context and tenant — so it is safe to register scoped alongside
/// the context it wraps.
/// </remarks>
public sealed class ErpApprovalService : IErpApprovalService
{
    private readonly IApprovalEngine<FM_TRANSACTION_MENU> _engine;

    /// <summary>Creates the service over the ERP's approval engine.</summary>
    public ErpApprovalService(IApprovalEngine<FM_TRANSACTION_MENU> engine)
        => _engine = engine ?? throw new ArgumentNullException(nameof(engine));

    /// <inheritdoc />
    public Task<IApprovable?> GetAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => _engine.GetApprovableByDocTypeAsync(docType, transId, cancellationToken);

    /// <inheritdoc />
    public Task<IApprovable> SubmitAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => _engine.ApplyApprovalAsync(docType, transId, ApprovalAction.Submit, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<IApprovable> ApproveAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => _engine.ApplyApprovalAsync(docType, transId, ApprovalAction.Approve, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<IApprovable> RejectAsync(string docType, object transId, CancellationToken cancellationToken = default)
        => _engine.ApplyApprovalAsync(docType, transId, ApprovalAction.Reject, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<IApprovable> ReworkAsync(string docType, object transId, int targetLevel, CancellationToken cancellationToken = default)
        => _engine.ApplyApprovalAsync(docType, transId, ApprovalAction.Rework, targetLevel, cancellationToken);
}
