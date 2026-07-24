using Alkanzi.Auditable;
using Alkanzi.Auditable.EntityFrameworkCore;

namespace Alkanzi.ErpServices;

/// <summary>
/// The ERP's document-type registry: each row names the table a document type's
/// transactions live in, scoped to a tenant.
/// </summary>
/// <remarks>
/// Only the columns dispatch reads are mapped. The registry has many more, but
/// the approval path needs just the code it is looked up by, the tenant it is
/// configured for, and the table it points at.
/// </remarks>
public class FM_TRANSACTION_MENU : ITransactionMenu
{
    public int ID { get; set; }
    public string DOC_TYPE { get; set; } = string.Empty;
    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int? BRANCH_ID { get; set; }
    public string? TABLE_NAME { get; set; }
}

/// <summary>
/// Journal voucher header — the table <c>JournalVoucher</c> dispatches to.
/// </summary>
/// <remarks>
/// Approvable but not workflow-bound: it carries APPROVE_STATUS, APPROVE_LEVEL
/// and DIGIT_SIGNATURE, but no WORKFLOW_ID.
/// </remarks>
public class FM_JOURNAL_HDR : IAuditable, IApprovable
{
    public int ID { get; set; }
    public int JV_NO { get; set; }
    public string? DOC_TYPE { get; set; }
    public int DOC_NUM { get; set; }
    public string? NARRATION { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}

/// <summary>
/// Call registration header — the table <c>callRegistration</c> dispatches to.
/// </summary>
/// <remarks>
/// Both approvable and workflow-bound: unlike <see cref="FM_JOURNAL_HDR"/> it
/// carries WORKFLOW_ID. Deliberately mapped with only its key and the audit,
/// approval and workflow columns — the nine schemas holding a
/// <c>CALL_REGISTERATION</c> carry materially different column sets, so anything
/// more has to be written against the one this connection resolves to.
/// </remarks>
public class CALL_REGISTERATION : IAuditable, IApprovable, IWorkflowBound
{
    public int ID { get; set; }

    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }
    public int? WORKFLOW_ID { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}
