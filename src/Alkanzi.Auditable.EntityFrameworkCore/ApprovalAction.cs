namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// What is being done to a transaction's approval state.
/// </summary>
/// <remarks>
/// The values are the codes stored in <see cref="IApprovable.APPROVE_STATUS"/>,
/// so an action and the status it leaves behind are the same number. Named here
/// rather than passed as a bare <see cref="int"/> so a caller cannot ask for
/// status 7, and so the transition each one performs has somewhere to be
/// documented.
/// </remarks>
public enum ApprovalAction
{
    /// <summary>Sent up the chain: climbs one level.</summary>
    Submit = 1,

    /// <summary>Sent back for correction: drops to the requested level.</summary>
    Rework = 2,

    /// <summary>Refused. The level is left where it stopped, recording who refused it.</summary>
    Reject = 3,

    /// <summary>Passed: climbs one level.</summary>
    Approve = 4,
}
