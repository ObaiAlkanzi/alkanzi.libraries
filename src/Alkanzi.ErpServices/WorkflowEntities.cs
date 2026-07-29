namespace Alkanzi.ErpServices;

/// <summary>
/// A workflow definition a document type can run through — the "form" in the
/// ERP's terms. Its <see cref="ID"/> is the <c>WF_ID</c> the workflow procedure
/// returns.
/// </summary>
public class SM_WORKFLOW_FORMS : IErpAuditable
{
    public int ID { get; set; }
    public string? NAME { get; set; }
    public string? TABLE_NAME { get; set; }
    public int DOC_ID { get; set; }
    public string? REAMRKS { get; set; }
    public string? INFO_QUERY { get; set; }

    /// <summary>The last (final) approval level of this workflow.</summary>
    public int LAST_LEVEL { get; set; }

    public bool NOTIFICATION { get; set; }

    /// <summary>The ERP's <c>Enums.ApprovalType</c>, kept as its numeric value here.</summary>
    public int APPROVAL_TYPE { get; set; }

    public string? REF_COL_VALUE { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

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
/// One approval level of a <see cref="SM_WORKFLOW_FORMS"/>, linked by
/// <see cref="FORM_ID"/>.
/// </summary>
public class SM_WORKFLOW_FORM_LEVELS : IErpAuditable
{
    public int ID { get; set; }
    public int LEVEL_ID { get; set; }
    public int ROLE_ID { get; set; }

    /// <summary>The <see cref="SM_WORKFLOW_FORMS.ID"/> this level belongs to.</summary>
    public int FORM_ID { get; set; }

    public int SECURITY_GROUP_ID { get; set; }
    public string? REMARKS { get; set; }
    public string? CC { get; set; }
    public string? BCC { get; set; }
    public bool? NO_OVERLAP { get; set; }
    public string? NO_OVERLAP_CONDITION { get; set; }
    public string? UPDATE_SENTENCE { get; set; }
    public string? SUBMIT_CONDITION { get; set; }
    public string? SUBMIT_PROCEDURE { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

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
/// The header of a transaction's approval log: one row per (document type,
/// transaction), tracking whether it has reached full approval. Each approval
/// action appends a <see cref="SM_APPROVAL_LOGS_DETAIL"/> under it.
/// </summary>
public class SM_APPROVAL_LOGS_HEADER : IErpAuditable
{
    public int ID { get; set; }

    /// <summary>The document type — the ERP's <c>DOC_TYPE</c> code.</summary>
    public string? DOC_NAME { get; set; }

    /// <summary>The document id (<see cref="SM_WORKFLOW_FORMS.DOC_ID"/>).</summary>
    public int DOC_ID { get; set; }

    /// <summary>The workflow form this transaction runs through, or 0 when it is not workflow-bound.</summary>
    public int FORM_ID { get; set; }

    /// <summary>The transaction's key.</summary>
    public int TRANSACTION_ID { get; set; }

    /// <summary>Set once the transaction reaches full approval.</summary>
    public bool IS_APPROVED { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

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
/// One entry in a transaction's approval log: the level the action was taken at
/// and the status it moved to. Belongs to a <see cref="SM_APPROVAL_LOGS_HEADER"/>
/// by <see cref="HDR_ID"/>.
/// </summary>
public class SM_APPROVAL_LOGS_DETAIL : IErpAuditable
{
    public int ID { get; set; }

    /// <summary>The <see cref="SM_APPROVAL_LOGS_HEADER.ID"/> this entry belongs to.</summary>
    public int HDR_ID { get; set; }

    public string? REMARKS { get; set; }

    /// <summary>The approval level the action was taken at.</summary>
    public int FROM_LEVEL { get; set; }

    /// <summary>The status the action moved the row to (an <see cref="ApprovalAction"/> code).</summary>
    public int APPROVE_STATUS { get; set; }

    public string? IP { get; set; }
    public string? HOST_NAME { get; set; }

    /// <summary>The level's name, taken from <see cref="SM_WORKFLOW_FORM_LEVELS.REMARKS"/>.</summary>
    public string? FROM_LEVEL_NAME { get; set; }

    public int ORG_ID { get; set; }
    public int COMP_ID { get; set; }
    public int BRANCH_ID { get; set; }

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
/// One row of what <c>APPROVAL_REVERT_PAK.GET_TRANS_WF</c> returns for a document
/// type: the workflow id, and — when several are configured — the function that
/// picks the right one for a given transaction.
/// </summary>
/// <param name="WfId">The workflow id (<see cref="SM_WORKFLOW_FORMS.ID"/>).</param>
/// <param name="MapFunction">
/// The <c>MAP_FUN</c> the cursor carries (e.g. <c>SM_DOC_WF_MAPPING.CALL_REGISTRATION</c>),
/// called as <c>fn(transId, docType)</c> to disambiguate when there is more than one.
/// </param>
/// <param name="MultiWorkflow">The cursor's <c>MULTI_WF</c> flag.</param>
public sealed record TransactionWorkflow(int WfId, string? MapFunction, bool MultiWorkflow);

/// <summary>
/// The single workflow resolved for a transaction: its id, final level, the form
/// and its levels.
/// </summary>
/// <param name="WfId">The resolved workflow id.</param>
/// <param name="FinalLevel">The form's <see cref="SM_WORKFLOW_FORMS.LAST_LEVEL"/>.</param>
/// <param name="Form">The workflow form.</param>
/// <param name="Levels">Its levels, ordered by <see cref="SM_WORKFLOW_FORM_LEVELS.LEVEL_ID"/>.</param>
public sealed record ResolvedWorkflow(
    int WfId,
    int FinalLevel,
    SM_WORKFLOW_FORMS Form,
    IReadOnlyList<SM_WORKFLOW_FORM_LEVELS> Levels);
