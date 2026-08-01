namespace Alkanzi.ErpServices;

/// <summary>
/// Why an approval attempt ended the way it did. The codes let callers branch on
/// the outcome instead of matching on <see cref="ApprovalResult.Message"/> text.
/// </summary>
public enum ApprovalOutcome
{
    /// <summary>The transition was applied and saved.</summary>
    Applied,

    /// <summary>No transaction row exists for the key, or it is soft-deleted.</summary>
    NotFound,

    /// <summary>The resolved table carries no approval columns, so it cannot be approved.</summary>
    NotApprovable,

    /// <summary>The row is already approved, so this action is not allowed.</summary>
    AlreadyApproved,

    /// <summary>The row is already rejected; it must be reworked before this action.</summary>
    AlreadyRejected,

    /// <summary>The acting user is not authorised to act on the transaction at its current level.</summary>
    NotAuthorized,

    /// <summary>The transaction is workflow-bound but no workflow is configured for it.</summary>
    NoWorkflow,

    /// <summary>The ERP approval procedure reported failure (its <c>0</c> flag); the work was rolled back.</summary>
    ProcessFailed,
}

/// <summary>
/// Everything the host needs to send its own approval notification (e.g.
/// <c>alkanziEmailServiceI.Post</c>) after an applied action — the engine sends
/// nothing itself. Carried on <see cref="ApprovalResult.Notification"/> for an
/// <see cref="ApprovalOutcome.Applied"/> result.
/// </summary>
/// <param name="DocType">The document type.</param>
/// <param name="TransId">The transaction id.</param>
/// <param name="WorkflowId">The workflow id (0 when not workflow-bound).</param>
/// <param name="ActingUser">The user who took the action.</param>
/// <param name="FromLevel">The level the action was taken at.</param>
/// <param name="ToLevel">The level the row moved to.</param>
/// <param name="Status">The status the row moved to (an <see cref="ApprovalAction"/> code).</param>
/// <param name="MainDocType">The main document type (<c>FM_TRANSACTION_MENU.MAIN_DOC_TYPE</c>).</param>
/// <param name="BranchId">The transaction's branch.</param>
/// <param name="DisplayName">The document type's display name.</param>
/// <param name="Initiator">The user who created the transaction (the row's <c>CREATED_BY</c>).</param>
/// <param name="TransRemarks">The transaction's own remarks (the row's <c>REMARKS</c>), if any.</param>
public sealed record ApprovalNotification(
    string DocType,
    int TransId,
    int WorkflowId,
    int ActingUser,
    int FromLevel,
    int ToLevel,
    int Status,
    string? MainDocType,
    int BranchId,
    string? DisplayName,
    int Initiator,
    string? TransRemarks);

/// <summary>
/// The result of an approval attempt: whether it succeeded, why, and the row it
/// concerned.
/// </summary>
/// <remarks>
/// Returned instead of thrown for outcomes that are ordinary answers to a user
/// action — not found, already approved, already rejected, not approvable — so a
/// caller can render a message or map to an HTTP status without a
/// <see langword="try"/>/<see langword="catch"/>. Genuine misuse (an invalid
/// action, a bad <c>targetLevel</c>) and misconfiguration (an unmapped table)
/// still throw.
/// </remarks>
public sealed record ApprovalResult
{
    private ApprovalResult(ApprovalOutcome outcome, IErpApprovable? row, string message, ApprovalNotification? notification = null)
    {
        Outcome = outcome;
        Row = row;
        Message = message;
        Notification = notification;
    }

    /// <summary>What happened.</summary>
    public ApprovalOutcome Outcome { get; }

    /// <summary>
    /// The transaction row. Present for <see cref="ApprovalOutcome.Applied"/>,
    /// <see cref="ApprovalOutcome.AlreadyApproved"/> and
    /// <see cref="ApprovalOutcome.AlreadyRejected"/> (its current state);
    /// <see langword="null"/> when there was no row to act on.
    /// </summary>
    public IErpApprovable? Row { get; }

    /// <summary>A human-readable explanation, for display or logging.</summary>
    public string Message { get; }

    /// <summary><see langword="true"/> only when the transition was applied.</summary>
    public bool Status => Outcome == ApprovalOutcome.Applied;

    /// <summary>
    /// The data to send a notification for this action — present only for
    /// <see cref="ApprovalOutcome.Applied"/>; <see langword="null"/> otherwise.
    /// </summary>
    public ApprovalNotification? Notification { get; }

    internal static ApprovalResult Applied(IErpApprovable row, ApprovalNotification notification)
        => new(ApprovalOutcome.Applied, row, "Approval applied.", notification);

    internal static ApprovalResult NotFound(string message)
        => new(ApprovalOutcome.NotFound, null, message);

    internal static ApprovalResult NotApprovable(string message)
        => new(ApprovalOutcome.NotApprovable, null, message);

    internal static ApprovalResult AlreadyApproved(IErpApprovable row, string message)
        => new(ApprovalOutcome.AlreadyApproved, row, message);

    internal static ApprovalResult AlreadyRejected(IErpApprovable row, string message)
        => new(ApprovalOutcome.AlreadyRejected, row, message);

    internal static ApprovalResult NotAuthorized(IErpApprovable row, string message)
        => new(ApprovalOutcome.NotAuthorized, row, message);

    internal static ApprovalResult NoWorkflow(IErpApprovable row, string message)
        => new(ApprovalOutcome.NoWorkflow, row, message);

    internal static ApprovalResult ProcessFailed(IErpApprovable row, string message)
        => new(ApprovalOutcome.ProcessFailed, row, message);
}
