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
    private ApprovalResult(ApprovalOutcome outcome, IErpApprovable? row, string message)
    {
        Outcome = outcome;
        Row = row;
        Message = message;
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

    internal static ApprovalResult Applied(IErpApprovable row)
        => new(ApprovalOutcome.Applied, row, "Approval applied.");

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
