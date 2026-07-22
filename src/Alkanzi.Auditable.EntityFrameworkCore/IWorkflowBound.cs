namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// A transaction row tied to a specific workflow definition.
/// </summary>
/// <remarks>
/// Separate from <see cref="IApprovable"/> on purpose. Approval status and level
/// are near-universal across the tables a document-type registry dispatches to,
/// but <c>WORKFLOW_ID</c> is carried by only about half of them. Folding it into
/// <see cref="IApprovable"/> would lock every table without the column out of
/// the interface entirely, so it is opted into separately by the entities that
/// have it.
/// </remarks>
public interface IWorkflowBound
{
    /// <summary>Workflow definition this row runs through.</summary>
    int? WORKFLOW_ID { get; set; }
}
