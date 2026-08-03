using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
/// <remarks>
/// Dispatch resolves a document type to a table through the registry, then a
/// table name to a mapped CLR type through EF's model — so a new transaction
/// table needs only to be mapped on <see cref="ErpDbContext"/>, not named here.
/// Saving runs through whatever interceptor the context carries, so approvals
/// are audit-stamped without this engine knowing about auditing.
/// </remarks>
public sealed class ErpApprovalEngine : IErpApprovalEngine
{
    // The ERP's signature key (Flexion's Consts.userSecurityKey). Must match the
    // ERP so DIGIT_SIGNATURE values verify on both sides.
    private const string EncryptionKey = "b14ca2e916";

    private readonly DbContext _context;
    private readonly IErpProcedureService _procedures;
    private readonly IErpUserProvider? _userProvider;
    private readonly IErpApprovalProcessService _process;

    /// <summary>Creates the engine over any EF context.</summary>
    /// <remarks>
    /// The context can be the host's own <see cref="DbContext"/> (e.g. the ERP's
    /// application context): the engine resolves and updates the transaction row
    /// through EF, and reads/writes the approval-infrastructure tables
    /// (FM_TRANSACTION_MENU, the workflow, log and history tables) with raw SQL on
    /// that same connection — so the host needs no package-specific entity types.
    /// <paramref name="procedures"/> is optional: when omitted, one is built over
    /// the same context. <paramref name="userProvider"/> supplies the acting user id
    /// for level authorization; when omitted, the acting user is taken as 0.
    /// <paramref name="approvalProcess"/> runs the ERP approval procedures that
    /// apply the transition; when omitted, one is built over the same context.
    /// </remarks>
    public ErpApprovalEngine(
        DbContext context,
        IErpProcedureService? procedures = null,
        IErpUserProvider? userProvider = null,
        IErpApprovalProcessService? approvalProcess = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _procedures = procedures ?? new ErpProcedureService(context);
        _userProvider = userProvider;
        _process = approvalProcess ?? new ErpApprovalProcessService(context, _procedures);
    }

    /// <inheritdoc />
    public async Task<IErpTransactionMenu?> GetMenuAsync(string docType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(docType);

        // Resolve the registry row through APPROVAL_REVERT_PAK.GET_TRANS_WF, which
        // joins the menu to its workflow form(s). It returns one row per workflow;
        // the menu fields (TABLE_NAME, DISPLAY_NAME, MAIN_DOC_TYPE, BRANCH_ID) are the
        // same on each, so the first row carries the menu. ORG_ID / COMP_ID are not
        // returned (the engine takes tenant from the transaction row, not the menu).
        var rows = await _procedures.QueryAsync(
            "APPROVAL_REVERT_PAK.GET_TRANS_WF",
            cursorParameter: "MCURSOR",
            map: reader => new ErpMenu(
                Str(reader, "DOC_TYPE") ?? string.Empty,
                0,
                0,
                IsNull(reader, "BRANCH_ID") ? null : Int(reader, "BRANCH_ID"),
                Str(reader, "TABLE_NAME"),
                Str(reader, "DISPLAY_NAME"),
                Str(reader, "MAIN_DOC_TYPE")),
            parameters: new Dictionary<string, object?> { ["DOC"] = docType },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<object?> GetTransactionAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transId);

        var menu = await GetMenuAsync(docType, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No {nameof(FM_TRANSACTION_MENU)} row is configured for document type '{docType}'.");

        if (string.IsNullOrWhiteSpace(menu.TABLE_NAME))
        {
            throw new InvalidOperationException(
                $"Document type '{docType}' has no TABLE_NAME, so its transaction table is unknown. " +
                "Most registry rows configure numbering and workflow only, and cannot be dispatched to a transaction.");
        }

        // Read the row's approval / tenant / audit columns by table name with raw
        // SQL — no entity type, no context model. Null when missing or soft-deleted.
        return await ReadApprovableAsync(menu.TABLE_NAME!, transId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IErpApprovable?> GetAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        var row = await GetTransactionAsync(docType, transId, cancellationToken).ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        return row as IErpApprovable
            ?? throw new InvalidOperationException(
                $"Document type '{docType}' resolves to {row.GetType().Name}, which does not implement " +
                $"{nameof(IErpApprovable)}. That table carries no approval columns, so it cannot be approved.");
    }

    /// <inheritdoc />
    public Task<ApprovalResult> SubmitAsync(string docType, object transId, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Submit, remarks: remarks, sgId: sgId, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<ApprovalResult> ApproveAsync(string docType, object transId, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Approve, remarks: remarks, sgId: sgId, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<ApprovalResult> RejectAsync(string docType, object transId, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Reject, remarks: remarks, sgId: sgId, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public Task<ApprovalResult> ReworkAsync(string docType, object transId, int targetLevel, string? remarks = null, int sgId = 0, CancellationToken cancellationToken = default)
        => ApplyApprovalAsync(docType, transId, ApprovalAction.Rework, targetLevel, remarks: remarks, sgId: sgId, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async Task<ApprovalResult> ApplyApprovalAsync(
        string docType,
        object transId,
        ApprovalAction action,
        int targetLevel = 0,
        string? remarks = null,
        int sgId = 0,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(
                nameof(action),
                action,
                $"Not an approval action. Expected one of: {string.Join(", ", Enum.GetNames<ApprovalAction>())}.");
        }

        if (action is not ApprovalAction.Rework && targetLevel != 0)
        {
            throw new ArgumentException(
                $"{nameof(targetLevel)} applies only to {nameof(ApprovalAction.Rework)}, " +
                $"but {action} was requested with level {targetLevel}.",
                nameof(targetLevel));
        } 
        ArgumentOutOfRangeException.ThrowIfNegative(targetLevel);
        ArgumentNullException.ThrowIfNull(transId);
        #region Get the transaction & Entity row to approve
        //Get TransactionMenu row to resolve the document type to a table name, then the table name to a mapped CLR type.


        var menu = await GetMenuAsync(docType, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No {nameof(FM_TRANSACTION_MENU)} row is configured for document type '{docType}'.");

        if (string.IsNullOrWhiteSpace(menu.TABLE_NAME))
        {
            throw new InvalidOperationException(
                $"Document type '{docType}' has no TABLE_NAME, so its transaction table is unknown. " +
                "Most registry rows configure numbering and workflow only, and cannot be dispatched to a transaction.");
        }

        // Read the row's approval / tenant / audit columns by table name with raw
        // SQL — no entity type, no context model. Null when missing or soft-deleted.
        var row = await ReadApprovableAsync(menu.TABLE_NAME!, transId, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            return ApprovalResult.NotFound(
                $"No row with key '{transId}' exists for document type '{docType}', or it is soft-deleted.");
        }
        #endregion

        // Config errors (unconfigured document type, unmapped table) still throw
        // from here; a not-found / soft-deleted row comes back as null.
        //var entity = await GetTransactionAsync(docType, transId, cancellationToken).ConfigureAwait(false);


        // A terminal row (approved or rejected) accepts nothing but Rework — the
        // correction path that reopens it. Any other action is a routine "no".
        if (row.APPROVE_STATUS == (int)ApprovalAction.Approve)
        {
            return ApprovalResult.AlreadyApproved(row,
                $"The row with key '{transId}' for document type '{docType}' is already approved; " +
                $"a {action} action is not allowed.");
        } 
        if (row.APPROVE_STATUS == (int)ApprovalAction.Reject)
        {
            return ApprovalResult.AlreadyRejected(row,
                $"The row with key '{transId}' for document type '{docType}' is rejected; " +
                $"it must be reworked before a {action} action.");
        }

        // Resolve the workflow governing this transaction. A row entering the chain
        // — still at level 0, or not yet bound (WORKFLOW_ID null or 0) — is resolved
        // by document type (GET_TRANS_WF + MAP_FUN). A row already bound loads its
        // workflow directly by WORKFLOW_ID, skipping those round-trips.
        var entering = row.APPROVE_LEVEL == 0 || row.WORKFLOW_ID is null or 0;
        var workflow = entering
            ? await ResolveWorkflowAsync(docType, transId, cancellationToken).ConfigureAwait(false)
            : await LoadWorkflowAsync(row.WORKFLOW_ID!.Value, cancellationToken).ConfigureAwait(false);

        // A workflow-bound row with no workflow configured cannot be routed. Rows
        // that are approvable but not workflow-bound (e.g. a journal voucher) keep
        // moving without one.
        if (workflow is null )
        {
            return ApprovalResult.NoWorkflow(row,
                $"The row with key '{transId}' for document type '{docType}' is workflow-bound, " +
                "but no workflow is configured for it.");
        }

        // Stamp the resolved workflow id onto a row entering the chain, so every
        // later action dispatches to the same workflow. Checked before the level is
        // incremented below, so the level-0 test sees the current level.
        if (entering)
        {
            row.WORKFLOW_ID = workflow.WfId;
        }

        // Level authorization: gate the action on APPROVAL_REVERT_PAK.LVL_AUTHORIZATION
        // for the current level. A workflow is required to authorize against, so rows
        // with none (e.g. a journal voucher) are not gated. Not authorized comes back
        // as a result, not an exception.
        
        var auth = await IsAuthorizedAsync(
                workflow.WfId, row.APPROVE_LEVEL, sgId, _userProvider?.GetCurrentUserId() ?? 0,
                docType, transId, overlap: 0, cancellationToken).ConfigureAwait(false);

        if (!auth.Authorized)
        {
            return ApprovalResult.NotAuthorized(row, auth.Message);
        }

       
        int UserLevelId = row.APPROVE_LEVEL +1;
        var level = workflow?.Levels.FirstOrDefault(l => l.LEVEL_ID == UserLevelId);
        // The level the action is taken at, before the transition moves it.
        var fromLevel = row.APPROVE_LEVEL;
        // The status is the action's own code by construction.
        row.APPROVE_STATUS = (int)action;
        row.APPROVE_LEVEL = action switch
        {
            ApprovalAction.Submit or ApprovalAction.Approve => UserLevelId,
            ApprovalAction.Rework => targetLevel,

            // Reject leaves the level alone, recording which level refused it.
            _ => row.APPROVE_LEVEL,
        };
        string? UpdateSentence = action switch
        {
            ApprovalAction.Submit or ApprovalAction.Approve => level?.UPDATE_SENTENCE,
            ApprovalAction.Rework => targetLevel == 0 ? "DOC_STATUS = 0" : workflow?.Levels.FirstOrDefault(l => l.LEVEL_ID == targetLevel)?.UPDATE_SENTENCE,
             
            // Reject leaves the level alone, recording which level refused it.
            _ => string.Empty,
        };
        // Reaching the workflow's final level completes the chain — but only a
        // climbing action gets there by approving. A Reject or Rework that sits on
        // the final level keeps its own status. A full approval stamps a digital
        // signature on the row.
        if (action is ApprovalAction.Submit or ApprovalAction.Approve
            && row.APPROVE_LEVEL == workflow?.FinalLevel)
        {
            row.APPROVE_STATUS = (int)ApprovalAction.Approve;
            row.DIGIT_SIGNATURE = EncryptString(Convert.ToInt32(transId).ToString());
        }
        var actingUser = _userProvider?.GetCurrentUserId() ?? 0;
        var id = Convert.ToInt32(transId);
        var now = DateTime.Now;
        var tenant = row as IErpTenantScoped;
        // Oracle 'DD-MON-YY', e.g. 30-JUL-26.
        var docDate = row.DOC_DATE.ToString("dd-MMM-yy", CultureInfo.InvariantCulture).ToUpperInvariant();

        // Stamp the audit columns on the transition — no EF save runs, so the engine
        // sets them and writes them in the UPDATE below.
        row.IS_UPDATED = true;
        row.UPDATED_BY = actingUser;
        row.UPDATED_AT = now;

        // Apply the transition on the row with raw SQL, run the level's UPDATE_SENTENCE,
        // drive the ERP approval procedure, and write the log — all in one transaction
        // so a procedure failure (or any exception) rolls the whole process back.
        var ownTransaction = _context.Database.CurrentTransaction is null;
        var transaction = ownTransaction
            ? await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;
        try
        {
            // Persist the transition on the transaction table itself: the approval
            // columns, the audit columns, and the level's UPDATE_SENTENCE (trusted
            // ERP config, appended to the SET list). No entity, no SaveChanges.
            var setClause =
                "APPROVE_STATUS = :p_status, APPROVE_LEVEL = :p_level, WORKFLOW_ID = :p_wf, DIGIT_SIGNATURE = :p_sig, " +
                "IS_UPDATED = 1, UPDATED_BY = :p_uby, UPDATED_AT = :p_uat";
            if (!string.IsNullOrWhiteSpace(UpdateSentence))
            {
                setClause += ", " + UpdateSentence;
            }

            await ExecSqlAsync(
                $"UPDATE {menu.TABLE_NAME} SET {setClause} WHERE ID = :p_id",
                command =>
                {
                    command.Parameters.Add(new OracleParameter("p_status", OracleDbType.Int32) { Value = row.APPROVE_STATUS });
                    command.Parameters.Add(new OracleParameter("p_level", OracleDbType.Int32) { Value = row.APPROVE_LEVEL });
                    command.Parameters.Add(new OracleParameter("p_wf", OracleDbType.Int32) { Value = (object?)row.WORKFLOW_ID ?? DBNull.Value });
                    command.Parameters.Add(new OracleParameter("p_sig", OracleDbType.Varchar2) { Value = (object?)row.DIGIT_SIGNATURE ?? DBNull.Value });
                    command.Parameters.Add(new OracleParameter("p_uby", OracleDbType.Int32) { Value = actingUser });
                    command.Parameters.Add(new OracleParameter("p_uat", OracleDbType.Date) { Value = now });
                    command.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int32) { Value = id });
                },
                cancellationToken).ConfigureAwait(false);

            // STR_QUERY is null — the engine has already applied the row above; the
            // procedure drives the workflow stack and returns 'message,flag'.
            var process = await _process.RunAsync(
                action,
                query: null,
                mainDocType: menu.MAIN_DOC_TYPE,
                docType: docType,
                transId: id,
                approveStatus: row.APPROVE_STATUS,
                userId: actingUser,
                orgId: tenant?.ORG_ID ?? 0,
                compId: tenant?.COMP_ID ?? 0,
                branchId: tenant?.BRANCH_ID ?? 0,
                docDate: docDate,
                cancellationToken).ConfigureAwait(false);

            if (!process.Success)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }

                return ApprovalResult.ProcessFailed(row, process.Message);
            }

            // Record the action — approval log and trans history — inside this
            // transaction, so they commit with the approval.
            await WriteApprovalLogAsync(docType, transId, row, workflow, level, remarks, cancellationToken).ConfigureAwait(false);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }

        // Hand back everything the host needs to send its own notification (e.g.
        // alkanziEmailServiceI.Post) — the engine does no email itself.
        var notification = new ApprovalNotification(
            DocType: docType,
            TransId: id,
            WorkflowId: workflow?.WfId ?? 0,
            ActingUser: actingUser,
            FromLevel: fromLevel,
            ToLevel: row.APPROVE_LEVEL,
            Status: row.APPROVE_STATUS,
            MainDocType: menu.MAIN_DOC_TYPE,
            BranchId: tenant?.BRANCH_ID ?? 0,
            DisplayName: menu.DISPLAY_NAME,
            Initiator: (row as IErpAuditable)?.CREATED_BY ?? 0,
            TransRemarks:row.REMARKS);

        return ApprovalResult.Applied(row, notification);
    }

    /// <summary>
    /// Appends this approval action to the transaction's log: the header
    /// (<see cref="SM_APPROVAL_LOGS_HEADER"/>) is created the first time the
    /// transaction is logged and flipped to approved once the row is fully
    /// approved; a <see cref="SM_APPROVAL_LOGS_DETAIL"/> row records the level and
    /// status of every action beneath it. Audit columns are stamped by the
    /// context's interceptor on save, as everywhere else in this engine.
    /// </summary>
    private async Task WriteApprovalLogAsync(
        string docType,
        object transId,
        IErpApprovable row,
        ResolvedWorkflow? workflow,
        SM_WORKFLOW_FORM_LEVELS? level,
        string? remarks,
        CancellationToken cancellationToken)
    {
        var transactionId = Convert.ToInt32(transId);
        var fullyApproved = row.APPROVE_STATUS == (int)ApprovalAction.Approve;

        // Tenant is taken from the transaction row itself when it carries the
        // columns (IErpTenantScoped) — not passed in from an ambient context. A
        // row without them logs 0 / null.
        var tenant = row as IErpTenantScoped;
        var orgId = tenant?.ORG_ID ?? 0;
        var compId = tenant?.COMP_ID ?? 0;
        var branchId = tenant?.BRANCH_ID ?? 0;
        var userId = _userProvider?.GetCurrentUserId() ?? 0;

        var now = DateTime.Now;

        // Header: one per (doc type, transaction). Created the first time, flipped to
        // approved once the row is fully approved. Raw SQL, so the host context needs
        // no package log types.
        var headers = await QuerySqlAsync(
            "SELECT ID, IS_APPROVED FROM SM_APPROVAL_LOGS_HEADER WHERE DOC_NAME = :doc AND TRANSACTION_ID = :tid",
            command =>
            {
                command.Parameters.Add(new OracleParameter("doc", OracleDbType.Varchar2) { Value = docType });
                command.Parameters.Add(new OracleParameter("tid", OracleDbType.Int32) { Value = transactionId });
            },
            reader => new { Id = Int(reader, "ID"), Approved = Int(reader, "IS_APPROVED") == 1 },
            cancellationToken).ConfigureAwait(false);

        int headerId;
        if (headers.Count == 0)
        {
            headerId = await InsertReturningIdAsync(
                "INSERT INTO SM_APPROVAL_LOGS_HEADER " +
                "(DOC_NAME, DOC_ID, FORM_ID, TRANSACTION_ID, IS_APPROVED, ORG_ID, COMP_ID, BRANCH_ID, CREATED_BY, CREATED_AT, IS_DELETED, IS_UPDATED) " +
                "VALUES (:doc, :docId, :formId, :tid, :appr, :org, :comp, :branch, :usr, :now, 0, 0) RETURNING ID INTO :id",
                command =>
                {
                    command.Parameters.Add(new OracleParameter("doc", OracleDbType.Varchar2) { Value = docType });
                    command.Parameters.Add(new OracleParameter("docId", OracleDbType.Int32) { Value = workflow?.Form.DOC_ID ?? 0 });
                    command.Parameters.Add(new OracleParameter("formId", OracleDbType.Int32) { Value = workflow?.WfId ?? 0 });
                    command.Parameters.Add(new OracleParameter("tid", OracleDbType.Int32) { Value = transactionId });
                    command.Parameters.Add(new OracleParameter("appr", OracleDbType.Int32) { Value = fullyApproved ? 1 : 0 });
                    command.Parameters.Add(new OracleParameter("org", OracleDbType.Int32) { Value = orgId });
                    command.Parameters.Add(new OracleParameter("comp", OracleDbType.Int32) { Value = compId });
                    command.Parameters.Add(new OracleParameter("branch", OracleDbType.Int32) { Value = branchId });
                    command.Parameters.Add(new OracleParameter("usr", OracleDbType.Int32) { Value = userId });
                    command.Parameters.Add(new OracleParameter("now", OracleDbType.Date) { Value = now });
                    var id = new OracleParameter("id", OracleDbType.Int32) { Direction = ParameterDirection.Output };
                    command.Parameters.Add(id);
                    return id;
                },
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            headerId = headers[0].Id;
            if (fullyApproved && !headers[0].Approved)
            {
                await ExecSqlAsync(
                    "UPDATE SM_APPROVAL_LOGS_HEADER SET IS_APPROVED = 1 WHERE ID = :id",
                    command => command.Parameters.Add(new OracleParameter("id", OracleDbType.Int32) { Value = headerId }),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await ExecSqlAsync(
            "INSERT INTO SM_APPROVAL_LOGS_DETAIL " +
            "(HDR_ID, FROM_LEVEL, FROM_LEVEL_NAME, APPROVE_STATUS, REMARKS, ORG_ID, COMP_ID, BRANCH_ID, CREATED_BY, CREATED_AT, IS_DELETED, IS_UPDATED) " +
            "VALUES (:hdr, :lvl, :lvlName, :status, :rem, :org, :comp, :branch, :usr, :now, 0, 0)",
            command =>
            {
                command.Parameters.Add(new OracleParameter("hdr", OracleDbType.Int32) { Value = headerId });
                command.Parameters.Add(new OracleParameter("lvl", OracleDbType.Int32) { Value = row.APPROVE_LEVEL });
                command.Parameters.Add(new OracleParameter("lvlName", OracleDbType.Varchar2) { Value = (object?)level?.REMARKS ?? DBNull.Value });
                command.Parameters.Add(new OracleParameter("status", OracleDbType.Int32) { Value = row.APPROVE_STATUS });
                command.Parameters.Add(new OracleParameter("rem", OracleDbType.Varchar2) { Value = (object?)remarks ?? DBNull.Value });
                command.Parameters.Add(new OracleParameter("org", OracleDbType.Int32) { Value = orgId });
                command.Parameters.Add(new OracleParameter("comp", OracleDbType.Int32) { Value = compId });
                command.Parameters.Add(new OracleParameter("branch", OracleDbType.Int32) { Value = branchId });
                command.Parameters.Add(new OracleParameter("usr", OracleDbType.Int32) { Value = userId });
                command.Parameters.Add(new OracleParameter("now", OracleDbType.Date) { Value = now });
            },
            cancellationToken).ConfigureAwait(false);

        // Mirror the action into the ERP's history trail: ACTION = status,
        // TRANS_STATUS = level, STATUS_NAME = level name.
        await ExecSqlAsync(
            "INSERT INTO SM_TRANS_HISTORY " +
            "(DOC_TYPE, TRANS_ID, TRANS_STATUS, ACTION, STATUS_NAME, POSTED_BY, POST_DATE, IS_SUB) " +
            "VALUES (:doc, :tid, :tstatus, :action, :sname, :postedBy, :now, 0)",
            command =>
            {
                command.Parameters.Add(new OracleParameter("doc", OracleDbType.Varchar2) { Value = docType });
                command.Parameters.Add(new OracleParameter("tid", OracleDbType.Int32) { Value = transactionId });
                command.Parameters.Add(new OracleParameter("tstatus", OracleDbType.Int32) { Value = row.APPROVE_LEVEL });
                command.Parameters.Add(new OracleParameter("action", OracleDbType.Int32) { Value = row.APPROVE_STATUS });
                command.Parameters.Add(new OracleParameter("sname", OracleDbType.Varchar2) { Value = (object?)level?.REMARKS ?? DBNull.Value });
                command.Parameters.Add(new OracleParameter("postedBy", OracleDbType.Int32) { Value = userId });
                command.Parameters.Add(new OracleParameter("now", OracleDbType.Date) { Value = now });
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TransactionWorkflow>> GetWorkflowsAsync(string docType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(docType);

        // APPROVAL_REVERT_PAK.GET_TRANS_WF(DOC IN VARCHAR2, MCURSOR OUT SYS_REFCURSOR).
        return _procedures.QueryAsync(
            "APPROVAL_REVERT_PAK.GET_TRANS_WF",
            cursorParameter: "MCURSOR",
            map: reader => new TransactionWorkflow(
                WfId: Convert.ToInt32(reader["WF_ID"]),
                MapFunction: reader["MAP_FUN"] as string,
                MultiWorkflow: reader["MULTI_WF"] is not (null or DBNull) && Convert.ToInt32(reader["MULTI_WF"]) == 1),
            parameters: new Dictionary<string, object?> { ["DOC"] = docType },
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResolvedWorkflow?> ResolveWorkflowAsync(string docType, object transId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transId);

        var workflows = await GetWorkflowsAsync(docType, cancellationToken).ConfigureAwait(false);
        if (workflows.Count == 0)
        {
            return null;
        }

        int? wfId;
        if (workflows.Count == 1)
        {
            wfId = workflows[0].WfId;
        }
        else
        {
            // Several workflows: the cursor's MAP_FUN names the function that
            // picks the one for this transaction.
            var mapFunction = workflows[0].MapFunction
                ?? throw new InvalidOperationException(
                    $"Document type '{docType}' has {workflows.Count} workflows but the cursor carried no MAP_FUN to disambiguate them.");

            wfId = await ResolveByMapFunctionAsync(mapFunction, transId, docType, cancellationToken).ConfigureAwait(false);
        }

        if (wfId is null or 0)
        {
            return null;
        }

        return await LoadWorkflowAsync(wfId.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a workflow form and its ordered levels by workflow id — the second
    /// half of resolution, reused when a row is already bound to a workflow.
    /// </summary>
    private async Task<ResolvedWorkflow?> LoadWorkflowAsync(int wfId, CancellationToken cancellationToken)
    {
        if (wfId == 0)
        {
            return null;
        }

        var forms = await QuerySqlAsync(
            "SELECT ID, DOC_ID, LAST_LEVEL FROM SM_WORKFLOW_FORMS WHERE ID = :wf",
            command => command.Parameters.Add(new OracleParameter("wf", OracleDbType.Int32) { Value = wfId }),
            reader => new SM_WORKFLOW_FORMS
            {
                ID = Int(reader, "ID"),
                DOC_ID = Int(reader, "DOC_ID"),
                LAST_LEVEL = Int(reader, "LAST_LEVEL"),
            },
            cancellationToken).ConfigureAwait(false);

        var form = forms.FirstOrDefault();
        if (form is null)
        {
            return null;
        }

        var levels = await QuerySqlAsync(
            "SELECT LEVEL_ID, UPDATE_SENTENCE, REMARKS FROM SM_WORKFLOW_FORM_LEVELS WHERE FORM_ID = :wf ORDER BY LEVEL_ID",
            command => command.Parameters.Add(new OracleParameter("wf", OracleDbType.Int32) { Value = wfId }),
            reader => new SM_WORKFLOW_FORM_LEVELS
            {
                FORM_ID = wfId,   // all rows belong to this form (the query's filter)
                LEVEL_ID = Int(reader, "LEVEL_ID"),
                UPDATE_SENTENCE = Str(reader, "UPDATE_SENTENCE"),
                REMARKS = Str(reader, "REMARKS"),
            },
            cancellationToken).ConfigureAwait(false);

        return new ResolvedWorkflow(wfId, form.LAST_LEVEL, form, levels);
    }

    /// <summary>
    /// Calls <c>APPROVAL_REVERT_PAK.LVL_AUTHORIZATION</c> to decide whether the
    /// acting user may act on a transaction at a level. The function returns
    /// <c>'flag,message'</c> — flag <c>1</c> authorized, <c>0</c> not.
    /// </summary>
    /// <remarks>
    /// The routine name is a fixed literal (not caller input); every argument is
    /// bound as a parameter.
    /// </remarks>
    private async Task<(bool Authorized, string Message)> IsAuthorizedAsync(
        int wfId, int transLevel, int sgId, int userId, string docType, object transId, int overlap, CancellationToken cancellationToken)
    {
        var raw = await _procedures.ExecuteAsync(
            "SELECT APPROVAL_REVERT_PAK.LVL_AUTHORIZATION(:p_wf, :p_lvl, :p_sg, :p_usr, :p_overlap, :p_doc, :p_trans) FROM DUAL",
            async command =>
            {
                var oracle = (OracleCommand)command;
                oracle.BindByName = true;
                oracle.Parameters.Add(new OracleParameter("p_wf", OracleDbType.Int32) { Value = wfId });
                oracle.Parameters.Add(new OracleParameter("p_lvl", OracleDbType.Int32) { Value = transLevel });
                oracle.Parameters.Add(new OracleParameter("p_sg", OracleDbType.Int32) { Value = sgId });
                oracle.Parameters.Add(new OracleParameter("p_usr", OracleDbType.Int32) { Value = userId });
                oracle.Parameters.Add(new OracleParameter("p_overlap", OracleDbType.Int32) { Value = overlap });
                oracle.Parameters.Add(new OracleParameter("p_doc", OracleDbType.Varchar2) { Value = docType });
                oracle.Parameters.Add(new OracleParameter("p_trans", OracleDbType.Int32) { Value = Convert.ToInt32(transId) });

                var result = await oracle.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result is null or DBNull ? null : result.ToString();
            },
            CommandType.Text,
            cancellationToken).ConfigureAwait(false);

        // Expect 'flag,message'; treat anything else as not authorized.
        var parts = (raw ?? "0,No authorization result").Split(',', 2);
        var authorized = parts[0].Trim() == "1";
        var message = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        return (authorized, message);
    }

    /// <summary>
    /// Calls the ERP mapping function named by the cursor's <c>MAP_FUN</c> —
    /// <c>SELECT fn(transId, docType) FROM DUAL</c> — to pick a single workflow.
    /// </summary>
    /// <remarks>
    /// The function name is interpolated because a routine name cannot be a bind
    /// parameter; it comes from ERP configuration (the cursor), not from a caller,
    /// so it is not an injection vector. The arguments are bound.
    /// </remarks>
    private Task<int?> ResolveByMapFunctionAsync(string mapFunction, object transId, string docType, CancellationToken cancellationToken)
        => _procedures.ExecuteAsync(
            $"SELECT {mapFunction}(:p_id, :p_doc) FROM DUAL",
            async command =>
            {
                var oracle = (OracleCommand)command;
                oracle.BindByName = true;
                oracle.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int32) { Value = Convert.ToInt32(transId) });
                oracle.Parameters.Add(new OracleParameter("p_doc", OracleDbType.Varchar2) { Value = docType });

                var result = await oracle.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result is null or DBNull ? (int?)null : Convert.ToInt32(result);
            },
            CommandType.Text,
            cancellationToken);

    /// <summary>
    /// Produces the digital signature stamped on a fully approved row — AES over
    /// the text, keyed the same way the ERP does so the signature verifies on both
    /// sides. The SHA1 / 1000-iteration derivation and salt match the ERP's scheme.
    /// </summary>
    private static string EncryptString(string clearText)
    {
        var clearBytes = Encoding.Unicode.GetBytes(clearText);
        var salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

        // 48 bytes = 32-byte key + 16-byte IV, taken in that order — the same bytes
        // the ERP's Rfc2898DeriveBytes(GetBytes(32) then GetBytes(16)) produced.
        var derived = Rfc2898DeriveBytes.Pbkdf2(EncryptionKey, salt, 1000, HashAlgorithmName.SHA1, 48);

        using var encryptor = Aes.Create();
        encryptor.Key = derived[..32];
        encryptor.IV = derived[32..];

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(clearBytes, 0, clearBytes.Length);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Reads a transaction row's approval / tenant / audit columns by table name.
    /// Returns null when the row is missing or soft-deleted. The column set is the
    /// standard one every approvable table carries — no entity type required.
    /// </summary>
    private async Task<RawApprovable?> ReadApprovableAsync(string tableName, object transId, CancellationToken cancellationToken)
    {
        var rows = await QuerySqlAsync(
            "SELECT APPROVE_STATUS, APPROVE_LEVEL, WORKFLOW_ID, DIGIT_SIGNATURE, DOC_DATE, REMARKS, " +
            $"DOC_TYPE, ORG_ID, COMP_ID, BRANCH_ID, CREATED_BY, IS_DELETED FROM {tableName.Trim()} WHERE ID = :id",
            command => command.Parameters.Add(new OracleParameter("id", OracleDbType.Int32) { Value = Convert.ToInt32(transId) }),
            reader => new RawApprovable
            {
                APPROVE_STATUS = Int(reader, "APPROVE_STATUS"),
                APPROVE_LEVEL = Int(reader, "APPROVE_LEVEL"),
                WORKFLOW_ID = IsNull(reader, "WORKFLOW_ID") ? null : Int(reader, "WORKFLOW_ID"),
                DIGIT_SIGNATURE = Str(reader, "DIGIT_SIGNATURE"),
                DOC_DATE = Date(reader, "DOC_DATE"),
                REMARKS = Str(reader, "REMARKS"),
                DOC_TYPE = Str(reader, "DOC_TYPE"),
                ORG_ID = Int(reader, "ORG_ID"),
                COMP_ID = Int(reader, "COMP_ID"),
                BRANCH_ID = Int(reader, "BRANCH_ID"),
                CREATED_BY = Int(reader, "CREATED_BY"),
                IS_DELETED = !IsNull(reader, "IS_DELETED") && Int(reader, "IS_DELETED") == 1,
            },
            cancellationToken).ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        return row is { IS_DELETED: true } ? null : row;
    }

    private static DateTime Date(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? default : Convert.ToDateTime(reader.GetValue(i));
    }

    // A transaction row's approval state, read by raw SQL — implements the approval,
    // workflow, audit and tenant contracts so results/notifications carry it, with no
    // entity type. Audit columns are stamped on the transition and written back.
    private sealed class RawApprovable : IErpApprovable, IErpWorkflowBound, IErpAuditable, IErpTenantScoped
    {
        public int? WORKFLOW_ID { get; set; }
        public int APPROVE_STATUS { get; set; }
        public int APPROVE_LEVEL { get; set; }
        public string? DIGIT_SIGNATURE { get; set; }
        public DateTime DOC_DATE { get; set; }
        public string? REMARKS { get; set; }
        public string? DOC_TYPE { get; set; }

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

    // --- Raw-SQL access to the approval-infrastructure tables. Routed through the
    // --- procedure service, which runs on the context's connection and enlists in
    // --- its ambient transaction — so these share the engine's unit of work.

    private Task<List<T>> QuerySqlAsync<T>(
        string sql, Action<OracleCommand> bind, Func<DbDataReader, T> map, CancellationToken cancellationToken)
        => _procedures.ExecuteAsync(sql, async command =>
        {
            var oracle = (OracleCommand)command;
            oracle.BindByName = true;
            bind(oracle);

            var results = new List<T>();
            await using var reader = await oracle.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(map(reader));
            }

            return results;
        }, CommandType.Text, cancellationToken);

    private Task<int> ExecSqlAsync(string sql, Action<OracleCommand> bind, CancellationToken cancellationToken)
        => _procedures.ExecuteAsync(sql, async command =>
        {
            var oracle = (OracleCommand)command;
            oracle.BindByName = true;
            bind(oracle);
            return await oracle.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }, CommandType.Text, cancellationToken);

    private Task<int> InsertReturningIdAsync(
        string sql, Func<OracleCommand, OracleParameter> bind, CancellationToken cancellationToken)
        => _procedures.ExecuteAsync(sql, async command =>
        {
            var oracle = (OracleCommand)command;
            oracle.BindByName = true;
            var idParam = bind(oracle);
            await oracle.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return idParam.Value is OracleDecimal d ? d.ToInt32() : Convert.ToInt32(idParam.Value);
        }, CommandType.Text, cancellationToken);

    private static bool IsNull(DbDataReader reader, string column)
        => reader.IsDBNull(reader.GetOrdinal(column));

    private static string? Str(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
    }

    private static int Int(DbDataReader reader, string column)
    {
        var i = reader.GetOrdinal(column);
        return reader.IsDBNull(i) ? 0 : Convert.ToInt32(reader.GetValue(i));
    }

    // Registry row read via raw SQL; implements the menu contract so the engine
    // consumes it without any package entity type.
    private sealed record ErpMenu(
        string DOC_TYPE, int ORG_ID, int COMP_ID, int? BRANCH_ID,
        string? TABLE_NAME, string? DISPLAY_NAME, string? MAIN_DOC_TYPE) : IErpTransactionMenu;
}
