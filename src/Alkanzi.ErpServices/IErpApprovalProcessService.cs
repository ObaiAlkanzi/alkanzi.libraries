namespace Alkanzi.ErpServices;

/// <summary>
/// The outcome of an approval stored-procedure run: the message it reported and
/// whether it succeeded (the <c>1</c>/<c>0</c> flag the procedure returns).
/// </summary>
/// <param name="Success"><see langword="true"/> when the procedure returned flag <c>1</c>.</param>
/// <param name="Message">The message part the procedure returned (before the flag).</param>
public sealed record ApprovalProcessResult(bool Success, string Message);

/// <summary>
/// Runs the ERP's approval stored procedures — <c>SM_APPROVE_PROCESS</c> for most
/// actions and <c>SM_REJECT_PROCESS</c> for <see cref="ApprovalAction.Reject"/> —
/// which apply the caller's UPDATE and drive the workflow stack server-side.
/// </summary>
public interface IErpApprovalProcessService
{
    /// <summary>
    /// Runs the approval procedure for <paramref name="action"/> inside a
    /// transaction that is committed only when the procedure reports success
    /// (flag <c>1</c>); a failure flag or any exception rolls the work back.
    /// </summary>
    /// <param name="action">Reject calls <c>SM_REJECT_PROCESS</c>; anything else calls <c>SM_APPROVE_PROCESS</c>.</param>
    /// <param name="query">The <c>STR_QUERY</c> — the UPDATE the procedure executes, or null when the caller already applied the row.</param>
    /// <param name="mainDocType">The main document type (<c>DOC_NAME</c> / <c>MAIN_DOC_TYPE</c>), or null.</param>
    /// <param name="docType">The document type (<c>TRANS_DOC</c> / <c>DOC_TYPE</c>).</param>
    /// <param name="transId">The transaction id.</param>
    /// <param name="approveStatus">The target status (<c>APPR_STATUS</c>) — used by the approve procedure only.</param>
    /// <param name="userId">The acting user (<c>USR</c>).</param>
    /// <param name="orgId">Organisation (<c>TRANS_ORG</c>).</param>
    /// <param name="compId">Company (<c>TRANS_COMP</c>).</param>
    /// <param name="branchId">Branch (<c>TRANS_BRANCH</c>).</param>
    /// <param name="docDate">Document date (<c>TRANS_DOC_DATE</c>), or null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed <see cref="ApprovalProcessResult"/> (message + success flag).</returns>
    Task<ApprovalProcessResult> RunAsync(
        ApprovalAction action,
        string? query,
        string? mainDocType,
        string docType,
        int transId,
        int approveStatus,
        int userId,
        int orgId,
        int compId,
        int branchId,
        string? docDate,
        CancellationToken cancellationToken = default);
}
