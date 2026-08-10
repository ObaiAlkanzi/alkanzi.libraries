using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
public sealed class ErpApprovalDashboardService : IErpApprovalDashboardService
{
    private readonly IErpProcedureService _procedures;

    /// <summary>
    /// Creates the service over any EF context — used only for its connection. The
    /// registry (FM_TRANSACTION_MENU) and the transaction tables are read purely by
    /// table name with raw SQL, so the host's own application context works and no
    /// entity types are required. The procedure runner (department-employee panel)
    /// self-provisions when not supplied.
    /// </summary>
    public ErpApprovalDashboardService(DbContext context, IErpProcedureService? procedures = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        _procedures = procedures ?? new ErpProcedureService(context);
    }

    // Registry row read via raw SQL — no package FM_TRANSACTION_MENU type needed.
    private sealed record MenuRow(string DocType, string? TableName, string? DisplayName, string? MainDocType);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalDashboardRow>> GetDataAsync(
        IEnumerable<string> docTypes,
        ApprovalDashboardFilter filter = ApprovalDashboardFilter.All,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docTypes);

        var types = new HashSet<string>(
            docTypes.Where(t => !string.IsNullOrWhiteSpace(t)), StringComparer.OrdinalIgnoreCase);
         if (types.Count == 0)
        {
            return [];
        }

        // The registry is small; read it once with raw SQL. Gives both the accessible
        // menus (TABLE_NAME to dispatch on, DISPLAY_NAME / MAIN_DOC_TYPE to enrich
        // with) and the per-table doc-type counts used to decide filtering — no
        // package FM_TRANSACTION_MENU type required.
        var allMenus = await LoadMenusAsync(cancellationToken).ConfigureAwait(false);

        // A table is filtered by DOC_TYPE only when shared by more than one doc type.
        // Count across the WHOLE registry, so a shared table the caller has partial
        // access to is still filtered (else it leaks other doc types' rows); a
        // single-doc-type table is never filtered (its DOC_TYPE column may not exist).
        var docTypesByTable = allMenus
            .Where(m => !string.IsNullOrWhiteSpace(m.TableName))
            .GroupBy(m => m.TableName!.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Select(x => x.DocType).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Accessible menus, one per doc type (a doc type can have several tenant rows).
        var accessible = allMenus
            .Where(m => types.Contains(m.DocType) && !string.IsNullOrWhiteSpace(m.TableName))
            .GroupBy(m => m.DocType, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First());

        var rows = new List<ApprovalDashboardRow>();
        foreach (var menu in accessible)
        {
            var applyDocTypeFilter =
                docTypesByTable.TryGetValue(menu.TableName!.Trim().ToUpperInvariant(), out var count) && count > 1;

            rows.AddRange(await QueryTableAsync(menu, filter, applyDocTypeFilter, cancellationToken).ConfigureAwait(false));
        }

        return rows;
    }

    // --- Per-user pending approvals ---

    // A user's levels: their security groups -> the workflow levels those groups are
    // on -> the form -> the document type and its table. DISTINCT because the same
    // (form, level) commonly arrives through several security groups.
    private const string UserScopeSql = """
        SELECT DISTINCT
               C.ID            AS FORM_ID,
               B.LEVEL_ID      AS LEVEL_ID,
               C.NAME          AS WORKFLOW_NAME,
               C.LAST_LEVEL    AS LAST_LEVEL,
               C.TABLE_NAME    AS TABLE_NAME,
               D.DOC_TYPE      AS DOC_TYPE,
               D.DISPLAY_NAME  AS DISPLAY_NAME,
               D.MAIN_DOC_TYPE AS MAIN_DOC_TYPE
        FROM   SM_DIVISION_SECURITY_GROUPS_USERS G
               JOIN SM_WORKFLOW_LVL_SECURITY_GROUPS B ON B.SECURITY_GROUP_ID = G.SECURITY_GROUP_ID
               JOIN SM_WORKFLOW_FORMS C              ON C.ID = B.HDR_ID
               JOIN FM_TRANSACTION_MENU D            ON D.ID = C.DOC_ID
        WHERE  G.IS_DELETED = 0
          AND  G.USER_ID = :p_user
          AND  B.IS_DELETED = 0
          AND  C.IS_DELETED = 0
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserApprovalScope>> GetUserScopeAsync(
        int userId, CancellationToken cancellationToken = default)
        => await QuerySqlAsync(
            UserScopeSql,
            command => command.Parameters.Add(new OracleParameter("p_user", OracleDbType.Int32) { Value = userId }),
            reader => new UserApprovalScope(
                Int(reader, "FORM_ID"),
                Int(reader, "LEVEL_ID"),
                Str(reader, "WORKFLOW_NAME"),
                Int(reader, "LAST_LEVEL"),
                Str(reader, "TABLE_NAME"),
                Str(reader, "DOC_TYPE") ?? string.Empty,
                Str(reader, "DISPLAY_NAME"),
                Str(reader, "MAIN_DOC_TYPE")),
            cancellationToken).ConfigureAwait(false);

    // Oracle caps an IN list at 1000 entries; stay well under it.
    private const int PairChunkSize = 250;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalDashboardRow>> GetUserDataAsync(
        int userId,
        ApprovalDashboardFilter filter = ApprovalDashboardFilter.Pending,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetUserScopeAsync(userId, cancellationToken).ConfigureAwait(false);
        if (scope.Count == 0)
        {
            return [];
        }

        // A form maps to exactly one document type, so its id is enough to attribute a
        // transaction row back to its doc type without selecting DOC_TYPE from the
        // table (which many transaction tables do not have).
        var byForm = scope
            .GroupBy(s => s.FormId)
            .ToDictionary(g => g.Key, g => g.First());

        // Group by table: one query per table instead of one per (doc type, level).
        // The tables are heavily shared — PRF_TRANSACTIONS alone backs ~25 doc types —
        // so this is the difference between a handful of round trips and a hundred.
        var byTable = scope
            .Where(s => IsSafeTableName(s.TableName))
            .GroupBy(s => s.TableName!.Trim().ToUpperInvariant());

        var rows = new List<ApprovalDashboardRow>();
        foreach (var table in byTable)
        {
            var pairs = table
                .Select(s => (Form: s.FormId, Level: s.LevelId))
                .Distinct()
                .ToList();

            for (var i = 0; i < pairs.Count; i += PairChunkSize)
            {
                rows.AddRange(await QueryUserTableAsync(
                    table.Key,
                    pairs.GetRange(i, Math.Min(PairChunkSize, pairs.Count - i)),
                    byForm,
                    filter,
                    cancellationToken).ConfigureAwait(false));
            }
        }

        return rows;
    }

    // TABLE_NAME comes from ERP config and is interpolated into SQL, so it must be a
    // plain identifier. Live data has at least one row whose TABLE_NAME carries a
    // stray quote and tab ("\tPRF_TRANSACTIONS") — skip those rather than build
    // broken (or injectable) SQL from them.
    private static bool IsSafeTableName(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        return tableName.Trim().All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '$' or '#' or '.');
    }

    private async Task<List<ApprovalDashboardRow>> QueryUserTableAsync(
        string tableName,
        List<(int Form, int Level)> pairs,
        Dictionary<int, UserApprovalScope> byForm,
        ApprovalDashboardFilter filter,
        CancellationToken cancellationToken)
    {
        var where = new List<string> { "(IS_DELETED IS NULL OR IS_DELETED != 1)" };
        switch (filter)
        {
            case ApprovalDashboardFilter.Pending: where.Add("APPROVE_STATUS NOT IN (3, 4)"); break;   // 3 reject, 4 approve
            case ApprovalDashboardFilter.Approved: where.Add("APPROVE_STATUS = 4"); break;
            case ApprovalDashboardFilter.Rejected: where.Add("APPROVE_STATUS = 3"); break;
        }

        // Oracle's multi-column IN: the row is the user's only when BOTH the workflow
        // and the level match. A null WORKFLOW_ID never matches, which is correct —
        // there is no workflow to authorise against.
        var tuples = string.Join(", ", pairs.Select((_, i) => $"(:w{i}, :l{i})"));
        where.Add($"(WORKFLOW_ID, APPROVE_LEVEL) IN ({tuples})");

        var sql =
            "SELECT ID, DOC_DATE, APPROVE_STATUS, APPROVE_LEVEL, WORKFLOW_ID, CREATED_BY, CREATED_AT " +
            $"FROM {tableName} WHERE {string.Join(" AND ", where)}";

        try
        {
            return await QuerySqlAsync(
                sql,
                command =>
                {
                    for (var i = 0; i < pairs.Count; i++)
                    {
                        command.Parameters.Add(new OracleParameter($"w{i}", OracleDbType.Int32) { Value = pairs[i].Form });
                        command.Parameters.Add(new OracleParameter($"l{i}", OracleDbType.Int32) { Value = pairs[i].Level });
                    }
                },
                reader =>
                {
                    var formId = NullableInt(reader, "WORKFLOW_ID") ?? 0;
                    byForm.TryGetValue(formId, out var s);

                    return new ApprovalDashboardRow(
                        Int(reader, "ID"),
                        s?.DocType ?? string.Empty,
                        NullableDate(reader, "DOC_DATE"),
                        Int(reader, "APPROVE_STATUS"),
                        Int(reader, "APPROVE_LEVEL"),
                        formId,
                        Int(reader, "CREATED_BY"),
                        NullableDate(reader, "CREATED_AT") ?? default,
                        s?.DisplayName,
                        s?.MainDocType);
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OracleException)
        {
            // The table doesn't expose the expected approval columns — skip it rather
            // than fail the whole dashboard, as GetDataAsync does.
            return [];
        }
    }

    // The whole registry, read once with raw SQL.
    private Task<List<MenuRow>> LoadMenusAsync(CancellationToken cancellationToken)
        => QuerySqlAsync(
            "SELECT DOC_TYPE, TABLE_NAME, DISPLAY_NAME, MAIN_DOC_TYPE FROM FM_TRANSACTION_MENU",
            _ => { },
            reader => new MenuRow(
                Str(reader, "DOC_TYPE") ?? string.Empty,
                Str(reader, "TABLE_NAME"),
                Str(reader, "DISPLAY_NAME"),
                Str(reader, "MAIN_DOC_TYPE")),
            cancellationToken);

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

    // --- Department-employee panel (PANEL.DEPARTMENT_EMPLOYEES) ---

    // The proc: P_DEPARTMENT_ID (IN NUMBER) + OUT_CURSOR (OUT SYS_REFCURSOR).
    private const string DepartmentEmployeesRoutine = "PANEL.DEPARTMENT_EMPLOYEES";
    private const string DepartmentEmployeesCursor = "OUT_CURSOR";

    /// <inheritdoc />
    public Task<IReadOnlyList<DepartmentEmployee>> GetDepartmentEmployeesAsync(
        int departmentId, CancellationToken cancellationToken = default)
        => _procedures.QueryAsync(
            DepartmentEmployeesRoutine,
            DepartmentEmployeesCursor,
            MapDepartmentEmployee,
            parameters: new Dictionary<string, object?> { ["P_DEPARTMENT_ID"] = departmentId },
            cancellationToken: cancellationToken);

    // Map one cursor row by column name — tolerant of column order and of a column
    // being absent, so a query that omits DESIGNATION/STATUS never throws.
    private static DepartmentEmployee MapDepartmentEmployee(DbDataReader reader) => new(
        Int(reader, "ID"),
        Int(reader, "USER_ID"),
        Str(reader, "EMPLOYEE"),
        Str(reader, "PROFILE"),
        Str(reader, "DEPARTMENT_NAME"),
        Int(reader, "EMP_DEPARTMENT_ID"),
        Int(reader, "EMP_DESIGNATION_ID"),
        Str(reader, "DESIGNATION"),
        Str(reader, "STATUS"));

    private static int? Ordinal(DbDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    private static string? Str(DbDataReader reader, string column)
        => Ordinal(reader, column) is int i && !reader.IsDBNull(i) ? reader.GetValue(i)?.ToString() : null;

    private static int Int(DbDataReader reader, string column)
        => Ordinal(reader, column) is int i && !reader.IsDBNull(i) ? Convert.ToInt32(reader.GetValue(i)) : 0;

    // Reads a transaction table's approval rows by raw SQL. A shared table (several
    // doc types) is filtered by DOC_TYPE so only this one's rows return; a
    // single-doc-type table is not (its DOC_TYPE column may not exist). Soft-deleted
    // rows are excluded. A table lacking the expected columns is skipped rather than
    // failing the whole dashboard.
    private async Task<List<ApprovalDashboardRow>> QueryTableAsync(
        MenuRow menu, ApprovalDashboardFilter filter, bool applyDocTypeFilter, CancellationToken cancellationToken)
    {
        var where = new List<string> { "(IS_DELETED IS NULL OR IS_DELETED != 1)" };
        switch (filter)
        {
            case ApprovalDashboardFilter.Pending: where.Add("APPROVE_STATUS NOT IN (3, 4)"); break;   // 3 reject, 4 approve
            case ApprovalDashboardFilter.Approved: where.Add("APPROVE_STATUS = 4"); break;
            case ApprovalDashboardFilter.Rejected: where.Add("APPROVE_STATUS = 3"); break;
        }

        if (applyDocTypeFilter)
        {
            where.Add("DOC_TYPE = :dt");
        }

        var sql =
            "SELECT ID, DOC_DATE, APPROVE_STATUS, APPROVE_LEVEL, WORKFLOW_ID, CREATED_BY, CREATED_AT " +
            $"FROM {menu.TableName!.Trim()} WHERE {string.Join(" AND ", where)}";

        try
        {
            return await QuerySqlAsync(
                sql,
                command =>
                {
                    if (applyDocTypeFilter)
                    {
                        command.Parameters.Add(new OracleParameter("dt", OracleDbType.Varchar2) { Value = menu.DocType });
                    }
                },
                reader => new ApprovalDashboardRow(
                    Int(reader, "ID"),
                    menu.DocType,
                    NullableDate(reader, "DOC_DATE"),
                    Int(reader, "APPROVE_STATUS"),
                    Int(reader, "APPROVE_LEVEL"),
                    NullableInt(reader, "WORKFLOW_ID"),
                    Int(reader, "CREATED_BY"),
                    NullableDate(reader, "CREATED_AT") ?? default,
                    menu.DisplayName,
                    menu.MainDocType),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OracleException)
        {
            // The table doesn't expose the expected approval columns — skip it
            // rather than fail the whole dashboard.
            return [];
        }
    }

    private static int? NullableInt(DbDataReader reader, string column)
        => Ordinal(reader, column) is int i && !reader.IsDBNull(i) ? Convert.ToInt32(reader.GetValue(i)) : null;

    private static DateTime? NullableDate(DbDataReader reader, string column)
        => Ordinal(reader, column) is int i && !reader.IsDBNull(i) ? Convert.ToDateTime(reader.GetValue(i)) : null;
}
