namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// A transaction row that moves through approval: where it stands, how far up
/// the chain it has climbed, and who signed it.
/// </summary>
/// <remarks>
/// Implement this on your own transaction entities, the same way you implement
/// <see cref="IAuditable"/> — the engine then hands back an approvable row
/// without knowing the concrete type. It is deliberately optional: a handful of
/// tables the registry dispatches to carry no approval columns at all, so
/// dispatch itself never requires it.
/// <para>
/// Note for querying: EF translates member access against the mapped entity
/// type, not an interface it happens to implement. A predicate written over
/// this interface — <c>Where(x =&gt; x.APPROVE_STATUS == 1)</c> — will not
/// translate. Use <c>EF.Property&lt;int&gt;(e, "APPROVE_STATUS")</c>, as
/// <see cref="ApprovalEngine{TMenu}"/> does for the registry lookup.
/// </para>
/// </remarks>
public interface IApprovable
{
    /// <summary>Where the row stands in approval.</summary>
    int APPROVE_STATUS { get; set; }

    /// <summary>How far up the approval chain the row has climbed.</summary>
    int APPROVE_LEVEL { get; set; }

    /// <summary>Digital signature captured on approval, if any.</summary>
    string? DIGIT_SIGNATURE { get; set; }
}
