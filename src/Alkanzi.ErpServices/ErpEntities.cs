namespace Alkanzi.ErpServices;

/// <summary>
/// The ERP's document-type registry: each row names the table a document type's
/// transactions live in, scoped to a tenant.
/// </summary>
public class FM_TRANSACTION_MENU : IErpTransactionMenu
{
    public int ID { get; set; }
    public string DOC_TYPE { get; set; } = string.Empty;
    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int? BRANCH_ID { get; set; }
    public string? TABLE_NAME { get; set; }
    public string? MAP_FUN { get; set; }
    public string? DISPLAY_NAME { get; set; }
    public string? MAIN_DOC_TYPE { get; set; }
}

/// <summary>
/// Journal voucher header — the table <c>JournalVoucher</c> dispatches to.
/// Approvable but not workflow-bound.
/// </summary>
public class FM_JOURNAL_HDR : IErpAuditable, IErpApprovable, IErpTenantScoped
{
    public int ID { get; set; }
    public int JV_NO { get; set; }
    public string? DOC_TYPE { get; set; }
    public int DOC_NUM { get; set; }
    public string? NARRATION { get; set; }
    public DateTime DOC_DATE { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }
    public string? REMARKS { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
    public int? WORKFLOW_ID { get; set; }
}

/// <summary>
/// Call registration header — the table <c>callRegistration</c> dispatches to.
/// Both approvable and workflow-bound.
/// </summary>
public class CALL_REGISTERATION : IErpAuditable, IErpApprovable, IErpWorkflowBound, IErpTenantScoped
{
    public int ID { get; set; }

    public int APPROVE_STATUS { get; set; }
    public int APPROVE_LEVEL { get; set; }
    public string? DIGIT_SIGNATURE { get; set; }
    public int? WORKFLOW_ID { get; set; }
    public string? REMARKS { get; set; }
    public string? DOC_TYPE { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }
    public DateTime DOC_DATE { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}
