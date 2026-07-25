namespace Alkanzi.ErpServices;

/// <summary>
/// The audit columns every ERP transaction table carries. Owned by this project
/// so nothing here depends on the general-purpose auditing library.
/// </summary>
public interface IErpAuditable
{
    /// <summary>Set once the row has been edited after creation.</summary>
    bool? IS_UPDATED { get; set; }

    /// <summary>Soft-delete marker: <see langword="true"/> means deleted.</summary>
    bool? IS_DELETED { get; set; }

    /// <summary>User who created the row.</summary>
    int CREATED_BY { get; set; }

    /// <summary>User who last updated the row.</summary>
    int? UPDATED_BY { get; set; }

    /// <summary>User who soft-deleted the row.</summary>
    int? DELETED_BY { get; set; }

    /// <summary>When the row was created (UTC).</summary>
    DateTime CREATED_AT { get; set; }

    /// <summary>When the row was last updated (UTC).</summary>
    DateTime? UPDATED_AT { get; set; }

    /// <summary>When the row was soft-deleted (UTC).</summary>
    DateTime? DELETED_AT { get; set; }
}

/// <summary>A transaction row that moves through approval.</summary>
public interface IErpApprovable
{
    /// <summary>Where the row stands in approval.</summary>
    int APPROVE_STATUS { get; set; }

    /// <summary>How far up the approval chain the row has climbed.</summary>
    int APPROVE_LEVEL { get; set; }

    /// <summary>Digital signature captured on approval, if any.</summary>
    string? DIGIT_SIGNATURE { get; set; }
}

/// <summary>
/// A transaction row bound to a specific workflow definition. Optional: only
/// some tables carry <c>WORKFLOW_ID</c>.
/// </summary>
public interface IErpWorkflowBound
{
    /// <summary>Workflow definition this row runs through.</summary>
    int? WORKFLOW_ID { get; set; }
}

/// <summary>
/// A document-type registry row: the code it is looked up by, the tenant it is
/// configured for, and the table its transactions live in.
/// </summary>
public interface IErpTransactionMenu
{
    /// <summary>Document type code callers dispatch on.</summary>
    string DOC_TYPE { get; }

    /// <summary>Organisation this configuration belongs to.</summary>
    int ORG_ID { get; }

    /// <summary>Company this configuration belongs to.</summary>
    int COMP_ID { get; }

    /// <summary>Branch this configuration belongs to.</summary>
    int? BRANCH_ID { get; }

    /// <summary>Name of the table holding this document type's transactions, or null.</summary>
    string? TABLE_NAME { get; }
}

/// <summary>Supplies the current acting user for audit stamping.</summary>
public interface IErpUserProvider
{
    /// <summary>Current user id, or <see langword="null"/> if none.</summary>
    int? GetCurrentUserId();
}

/// <summary>Supplies the tenant approvals are scoped to.</summary>
public interface IErpCompanyContext
{
    /// <summary>Current organisation id.</summary>
    int ORG_ID { get; }

    /// <summary>Current company id.</summary>
    int COMP_ID { get; }

    /// <summary>Current branch id, or <see langword="null"/> to leave the lookup unscoped by branch.</summary>
    int? BRANCH_ID { get; }
}

/// <summary>
/// What is being done to a transaction's approval state. The values are the
/// codes stored in <see cref="IErpApprovable.APPROVE_STATUS"/>.
/// </summary>
public enum ApprovalAction
{
    /// <summary>Sent up the chain: climbs one level.</summary>
    Submit = 1,

    /// <summary>Sent back for correction: drops to the requested level.</summary>
    Rework = 2,

    /// <summary>Refused. The level is left where it stopped.</summary>
    Reject = 3,

    /// <summary>Passed: climbs one level.</summary>
    Approve = 4,
}
