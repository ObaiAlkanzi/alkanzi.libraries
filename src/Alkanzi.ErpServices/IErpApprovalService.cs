using Alkanzi.Auditable.EntityFrameworkCore;

namespace Alkanzi.ErpServices;

/// <summary>
/// The approval operations the ERP speaks in — submit, approve, reject, rework —
/// over any document type, dispatched to its transaction table for the current
/// tenant.
/// </summary>
/// <remarks>
/// A thin, named layer over <see cref="IApprovalEngine{TMenu}"/>: the ERP thinks
/// in verbs, not in a generic action code, so each verb is its own method and
/// the <see cref="ApprovalAction"/> mapping lives in one place.
/// </remarks>
public interface IErpApprovalService
{
    /// <summary>
    /// Loads a document's approval state, or <see langword="null"/> when the row
    /// does not exist or is soft-deleted.
    /// </summary>
    Task<IApprovable?> GetAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Sends a document up the chain: climbs one level, status becomes submitted.</summary>
    Task<IApprovable> SubmitAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Passes a document: climbs one level, status becomes approved.</summary>
    Task<IApprovable> ApproveAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Refuses a document: status becomes rejected, level records where it stopped.</summary>
    Task<IApprovable> RejectAsync(string docType, object transId, CancellationToken cancellationToken = default);

    /// <summary>Sends a document back for correction: status becomes rework, level drops to <paramref name="targetLevel"/>.</summary>
    Task<IApprovable> ReworkAsync(string docType, object transId, int targetLevel, CancellationToken cancellationToken = default);
}
