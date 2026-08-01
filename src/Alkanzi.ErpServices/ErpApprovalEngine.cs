using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

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

    private readonly ErpDbContext _context;
    private readonly IErpProcedureService _procedures;
    private readonly IErpUserProvider? _userProvider;
    private readonly IErpApprovalProcessService _process;

    /// <summary>Creates the engine over the ERP context.</summary>
    /// <remarks>
    /// <paramref name="procedures"/> is optional: when omitted, one is built over
    /// the same context, so callers that do not use the workflow procedures need
    /// not supply it. <paramref name="userProvider"/> supplies the acting user id
    /// for level authorization; when omitted, the acting user is taken as 0.
    /// <paramref name="approvalProcess"/> runs the ERP approval procedures that
    /// apply the transition; when omitted, one is built over the same context.
    /// </remarks>
    public ErpApprovalEngine(
        ErpDbContext context,
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

        // The concrete registry entity is queried directly, so its columns
        // translate without EF.Property indirection.
        return await _context.TransactionMenus
            .FirstOrDefaultAsync(m => m.DOC_TYPE == docType, cancellationToken)
            .ConfigureAwait(false);
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

        var clrType = ResolveClrType(menu.TABLE_NAME)
            ?? throw new InvalidOperationException(
                $"No entity type is mapped to table '{menu.TABLE_NAME}'. " +
                $"Map it on {nameof(ErpDbContext)} for document type '{docType}' to dispatch.");

        var entity = await _context.FindAsync(clrType, [transId], cancellationToken).ConfigureAwait(false);

        // The query filter already excludes soft-deleted rows from the SELECT
        // Find issues. It does not help when the row is already tracked, because
        // Find then returns it from the change tracker without querying.
        return entity is IErpAuditable { IS_DELETED: true } ? null : entity;
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

        //Get Entity row to approve, using the resolved CLR type and the provided transaction ID.
        var clrType = ResolveClrType(menu.TABLE_NAME)
            ?? throw new InvalidOperationException(
                $"No entity type is mapped to table '{menu.TABLE_NAME}'. " +
                $"Map it on {nameof(ErpDbContext)} for document type '{docType}' to dispatch."); 
        var entity = await _context.FindAsync(clrType, [transId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return ApprovalResult.NotFound(
                $"No row with key '{transId}' exists for document type '{docType}', or it is soft-deleted.");
        }
        if (entity is not IErpApprovable row)
        {
            return ApprovalResult.NotApprovable(
                $"Document type '{docType}' resolves to {entity.GetType().Name}, which has no approval columns, " +
                "so it cannot be approved.");
        }
        // The query filter already excludes soft-deleted rows from the SELECT
        // Find issues. It does not help when the row is already tracked, because
        // Find then returns it from the change tracker without querying.
        // return entity is IErpAuditable { IS_DELETED: true } ? null : entity;
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
        var tenant = row as IErpTenantScoped;
        // Oracle 'DD-MON-YY', e.g. 30-JUL-26.
        var docDate = row.DOC_DATE.ToString("dd-MMM-yy", CultureInfo.InvariantCulture).ToUpperInvariant();

        // Apply the transition through EF, run the level's UPDATE_SENTENCE, drive
        // the ERP approval procedure, and write the log — all in one transaction so
        // a procedure failure (or any exception) rolls the whole process back.
        var ownTransaction = _context.Database.CurrentTransaction is null;
        var transaction = ownTransaction
            ? await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;
        try
        {
            if (!string.IsNullOrWhiteSpace(UpdateSentence))
            {
                // The table name and SET fragment are trusted ERP config (not caller
                // input); transId is bound. Hence the EF1002 suppression.
#pragma warning disable EF1002 // raw SQL: interpolated parts are trusted ERP configuration
                await _context.Database
                    .ExecuteSqlRawAsync(
                        $"UPDATE {menu.TABLE_NAME} SET {UpdateSentence} WHERE ID = {{0}}",
                        [transId],
                        cancellationToken)
                    .ConfigureAwait(false);
#pragma warning restore EF1002
            }
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // STR_QUERY is null — the engine has already applied the row above; the
            // procedure drives the workflow stack and returns 'message,flag'.
            var process = await _process.RunAsync(
                action,
                query: null,
                mainDocType: (menu as FM_TRANSACTION_MENU)?.MAIN_DOC_TYPE,
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
            MainDocType: (menu as FM_TRANSACTION_MENU)?.MAIN_DOC_TYPE,
            BranchId: tenant?.BRANCH_ID ?? 0,
            DisplayName: (menu as FM_TRANSACTION_MENU)?.DISPLAY_NAME,
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

        var header = await _context.ApprovalLogHeaders
            .FirstOrDefaultAsync(
                h => h.DOC_NAME == docType && h.TRANSACTION_ID == transactionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (header is null)
        {
            header = new SM_APPROVAL_LOGS_HEADER
            {
                DOC_NAME = docType,
                DOC_ID = workflow?.Form.DOC_ID ?? 0,
                FORM_ID = workflow?.WfId ?? 0,
                TRANSACTION_ID = transactionId,
                IS_APPROVED = fullyApproved,
                ORG_ID = orgId,
                COMP_ID = compId,
                BRANCH_ID = branchId,
            };
            _context.ApprovalLogHeaders.Add(header);

            // Save so the store assigns the header key the detail must reference.
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (fullyApproved && !header.IS_APPROVED)
        {
            header.IS_APPROVED = true;
        }

        _context.ApprovalLogDetails.Add(new SM_APPROVAL_LOGS_DETAIL
        {
            HDR_ID = header.ID,
            FROM_LEVEL = row.APPROVE_LEVEL,
            FROM_LEVEL_NAME = level?.REMARKS,
            APPROVE_STATUS = row.APPROVE_STATUS,
            REMARKS = remarks,
            ORG_ID = orgId,
            COMP_ID = compId,
            BRANCH_ID = branchId,
        });

        // Mirror the action into the ERP's history trail — same shape the ERP
        // writes: ACTION = status, TRANS_STATUS = level, STATUS_NAME = level name.
        _context.TransHistory.Add(new SM_TRANS_HISTORY
        {
            DOC_TYPE = docType,
            TRANS_ID = transactionId,
            TRANS_STATUS = row.APPROVE_LEVEL,
            ACTION = row.APPROVE_STATUS,
            STATUS_NAME = level?.REMARKS,
            POSTED_BY = userId,
            POST_DATE = DateTime.Now,
            IS_SUB = false,
        });

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

        var form = await _context.WorkflowForms
            .FirstOrDefaultAsync(f => f.ID == wfId, cancellationToken)
            .ConfigureAwait(false);

        if (form is null)
        {
            return null;
        }

        var levels = await _context.WorkflowFormLevels
            .Where(l => l.FORM_ID == wfId)
            .OrderBy(l => l.LEVEL_ID)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

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
    /// Maps a table name to the CLR type mapped to it, through EF's model. Owned
    /// types and TPH subclasses share a table with their owner/root, so they are
    /// skipped to avoid a false match.
    /// </summary>
    private Type? ResolveClrType(string tableName)
    {
        var target = tableName.Trim();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            if (string.Equals(entityType.GetTableName(), target, StringComparison.OrdinalIgnoreCase))
            {
                return entityType.ClrType;
            }
        }

        return null;
    }
}
