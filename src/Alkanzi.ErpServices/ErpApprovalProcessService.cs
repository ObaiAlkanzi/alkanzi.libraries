using System.Data;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
public sealed class ErpApprovalProcessService : IErpApprovalProcessService
{
    private readonly DbContext _context;
    private readonly IErpProcedureService _procedures;

    /// <summary>Creates the service over any EF context's connection.</summary>
    /// <remarks>
    /// <paramref name="procedures"/> is optional: when omitted, one is built over
    /// the same context.
    /// </remarks>
    public ErpApprovalProcessService(DbContext context, IErpProcedureService? procedures = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _procedures = procedures ?? new ErpProcedureService(context);
    }

    /// <inheritdoc />
    public async Task<ApprovalProcessResult> RunAsync(
        ApprovalAction action,
        string? query,
        string? mainDocType,
        string docType,
        int transId,
        int approveStatus,
        int userId,
        int orgId,
        int compId,
        int branchId,
        string? docDate,
        CancellationToken cancellationToken = default)
    {
        var isReject = action == ApprovalAction.Reject;
        var routine = isReject ? "SM_REJECT_PROCESS" : "SM_APPROVE_PROCESS";

        // Own a transaction unless the caller already opened one, so the whole
        // process — the procedure's UPDATE and its workflow-stack work — commits
        // only on success and rolls back on a failure flag or an exception.
        var ownTransaction = _context.Database.CurrentTransaction is null;
        var transaction = ownTransaction
            ? await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        try
        {
            var raw = await _procedures.ExecuteAsync(
                routine,
                async command =>
                {
                    var oracle = (OracleCommand)command;
                    oracle.BindByName = true;

                    oracle.Parameters.Add(new OracleParameter("STR_QUERY", OracleDbType.Varchar2) { Value = (object?)query ?? DBNull.Value });
                    if (isReject)
                    {
                        oracle.Parameters.Add(new OracleParameter("MAIN_DOC_TYPE", OracleDbType.Varchar2) { Value = (object?)mainDocType ?? DBNull.Value });
                        oracle.Parameters.Add(new OracleParameter("DOC_TYPE", OracleDbType.Varchar2) { Value = docType });
                        oracle.Parameters.Add(new OracleParameter("TRANSACTION_ID", OracleDbType.Int32) { Value = transId });
                        oracle.Parameters.Add(new OracleParameter("USR", OracleDbType.Int32) { Value = userId });
                    }
                    else
                    {
                        oracle.Parameters.Add(new OracleParameter("DOC_NAME", OracleDbType.Varchar2) { Value = (object?)mainDocType ?? DBNull.Value });
                        oracle.Parameters.Add(new OracleParameter("TRANSACTION_ID", OracleDbType.Int32) { Value = transId });
                        oracle.Parameters.Add(new OracleParameter("APPR_STATUS", OracleDbType.Int32) { Value = approveStatus });
                        oracle.Parameters.Add(new OracleParameter("TRANS_DOC", OracleDbType.Varchar2) { Value = docType });
                        oracle.Parameters.Add(new OracleParameter("USR", OracleDbType.Int32) { Value = userId });
                    }

                    oracle.Parameters.Add(new OracleParameter("TRANS_ORG", OracleDbType.Int32) { Value = orgId });
                    oracle.Parameters.Add(new OracleParameter("TRANS_COMP", OracleDbType.Int32) { Value = compId });
                    oracle.Parameters.Add(new OracleParameter("TRANS_BRANCH", OracleDbType.Int32) { Value = branchId });
                    oracle.Parameters.Add(new OracleParameter("TRANS_DOC_DATE", OracleDbType.Varchar2) { Value = (object?)docDate ?? DBNull.Value });

                    var msg = new OracleParameter("MSG", OracleDbType.Varchar2, 4000, null, ParameterDirection.Output);
                    oracle.Parameters.Add(msg);

                    await oracle.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    return msg.Value is null or DBNull ? null : msg.Value.ToString();
                },
                CommandType.StoredProcedure,
                cancellationToken).ConfigureAwait(false);

            var result = Parse(raw);

            if (ownTransaction)
            {
                if (result.Success)
                {
                    await transaction!.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await transaction!.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            if (ownTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }

            return new ApprovalProcessResult(false, ex.Message);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    // The procedures return 'message,flag' — flag 1 success, 0 failure. The message
    // itself may contain commas, so split on the last one.
    private static ApprovalProcessResult Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ApprovalProcessResult(false, "The approval procedure returned no message.");
        }

        var comma = raw.LastIndexOf(',');
        if (comma < 0)
        {
            return new ApprovalProcessResult(false, raw.Trim());
        }

        var flag = raw[(comma + 1)..].Trim();
        var message = raw[..comma].Trim();
        return new ApprovalProcessResult(flag == "1", message);
    }
}
