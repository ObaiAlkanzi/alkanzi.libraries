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

    /// <summary>When the row was created (server-local).</summary>
    DateTime CREATED_AT { get; set; }

    /// <summary>When the row was last updated (server-local).</summary>
    DateTime? UPDATED_AT { get; set; }

    /// <summary>When the row was soft-deleted (server-local).</summary>
    DateTime? DELETED_AT { get; set; }

    /// <summary>Stamps the row as newly created by <paramref name="userId"/> (server-local now).</summary>
    void MarkCreated(int userId)
    {
        CREATED_BY = userId;
        CREATED_AT = DateTime.Now;
        IS_UPDATED = false;
        IS_DELETED = false;
    }

    /// <summary>Stamps the row as updated by <paramref name="userId"/> (server-local now).</summary>
    void MarkUpdated(int userId)
    {
        UPDATED_BY = userId;
        UPDATED_AT = DateTime.Now;
        IS_UPDATED = true;
    }

    /// <summary>Marks the row soft-deleted by <paramref name="userId"/> (server-local now).</summary>
    void MarkDeleted(int userId)
    {
        DELETED_BY = userId;
        DELETED_AT = DateTime.Now;
        IS_DELETED = true;
    }
}

/// <summary>A transaction row that moves through approval.</summary>
public interface IErpApprovable
{
    public int? WORKFLOW_ID { get; set; }
    /// <summary>Where the row stands in approval.</summary>
    int APPROVE_STATUS { get; set; }

    /// <summary>How far up the approval chain the row has climbed.</summary>
    int APPROVE_LEVEL { get; set; }

    /// <summary>Digital signature captured on approval, if any.</summary>
    string? DIGIT_SIGNATURE { get; set; }

    /// <summary>The document date, passed to the approval procedure as <c>TRANS_DOC_DATE</c>.</summary>
    /// <remarks>
    /// Non-nullable: every ERP transaction carries a document date (matches the
    /// host's <c>TRANSACTION_BASE.DOC_DATE</c>). Being a plain <see cref="DateTime"/>
    /// keeps it a mapped column the dashboard can project in a LINQ query.
    /// </remarks>
    DateTime DOC_DATE { get; set; }

    /// <summary>The transaction's own remarks, surfaced on the approval notification.</summary>
    string? REMARKS { get; set; }

    /// <summary>The document type the row belongs to.</summary>
    string? DOC_TYPE { get; set; }
    
}

/// <summary>
/// A transaction row that carries its own tenant columns. When a row implements
/// this, the approval engine takes <c>ORG_ID</c> / <c>COMP_ID</c> / <c>BRANCH_ID</c>
/// for the approval log from the row itself (what <c>GetTransactionAsync</c> loads).
/// </summary>
public interface IErpTenantScoped
{
    /// <summary>Organisation the transaction belongs to.</summary>
    int ORG_ID { get; }

    /// <summary>Company the transaction belongs to.</summary>
    int COMP_ID { get; }

    /// <summary>Branch the transaction belongs to.</summary>
    int BRANCH_ID { get; }
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

    /// <summary>Human-readable name for the document type, or null.</summary>
    string? DISPLAY_NAME { get; }

    /// <summary>The parent/main document type this one rolls up to, or null.</summary>
    string? MAIN_DOC_TYPE { get; }
}

/// <summary>Supplies the current acting user for audit stamping.</summary>
public interface IErpUserProvider
{
    /// <summary>Current user id, or <see langword="null"/> if none.</summary>
    int? GetCurrentUserId();
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
