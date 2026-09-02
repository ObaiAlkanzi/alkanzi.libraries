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

    // A user's security groups (id + name). GROUP BY collapses a group reached
    // through several division rows so it appears once. LEFT JOIN so a group with no
    // master row still returns (NAME null).
    private const string UserSecurityGroupsSql = """
        SELECT   A.SECURITY_GROUP_ID AS SECURITY_GROUP_ID,
                 B.NAME              AS NAME
        FROM     SM_DIVISION_SECURITY_GROUPS_USERS A
                 LEFT JOIN SM_SECURITY_GROUPS_MASTER B ON B.ID = A.SECURITY_GROUP_ID
        WHERE    A.IS_DELETED = 0
          AND    A.USER_ID = :p_user
        GROUP BY A.SECURITY_GROUP_ID, B.NAME
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSecurityGroup>> GetUserSecurityGroupsAsync(
        int userId, CancellationToken cancellationToken = default)
        => await QuerySqlAsync(
            UserSecurityGroupsSql,
            command => command.Parameters.Add(new OracleParameter("p_user", OracleDbType.Int32) { Value = userId }),
            reader => new UserSecurityGroup(
                Int(reader, "SECURITY_GROUP_ID"),
                Str(reader, "NAME")),
            cancellationToken).ConfigureAwait(false);

    // The workflow forms a security group sits on. DISTINCT because a group is
    // commonly on several levels of the same form. The join is written LEFT but
    // B.IS_DELETED = 0 makes it behave as an inner join — a level row pointing at a
    // missing or deleted form is dropped, which is what we want here.
    private const string SecurityGroupWorkflowsSql = """
        SELECT DISTINCT
               B.ID                AS WF_ID,
               B.NAME              AS NAME,
               A.SECURITY_GROUP_ID AS SECURITY_GROUP_ID
        FROM   SM_WORKFLOW_LVL_SECURITY_GROUPS A
               LEFT JOIN SM_WORKFLOW_FORMS B ON B.ID = A.HDR_ID
        WHERE  A.IS_DELETED = 0
          AND  B.IS_DELETED = 0
          AND  A.SECURITY_GROUP_ID = :p_sg
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecurityGroupWorkflow>> GetSecurityGroupWorkflowsAsync(
        int securityGroupId, CancellationToken cancellationToken = default)
        => await QuerySqlAsync(
            SecurityGroupWorkflowsSql,
            command => command.Parameters.Add(new OracleParameter("p_sg", OracleDbType.Int32) { Value = securityGroupId }),
            reader => new SecurityGroupWorkflow(
                Int(reader, "WF_ID"),
                Str(reader, "NAME"),
                Int(reader, "SECURITY_GROUP_ID")),
            cancellationToken).ConfigureAwait(false);

    // The members of a security group, with the group and division names. The user
    // join is written LEFT but B."IsDeleted" = 0 makes it behave as an inner join —
    // a membership row pointing at a missing or deleted user is dropped. The group
    // and division joins stay genuinely outer, so a membership whose master or
    // division row is gone still returns with a null name. No DISTINCT: membership
    // is per division, so a user in the group under two divisions is legitimately
    // two rows.
    private const string SecurityGroupUsersSql = """
        SELECT A.USER_ID                             AS USER_ID,
               B."First_Name" || ' ' || B."Last_Name" AS USER_NAME,
               C.NAME                                AS SG_NAME,
               D.NAME                                AS DIVISION_NAME,
               A.SECURITY_GROUP_ID                   AS SECURITY_GROUP_ID
        FROM   SM_DIVISION_SECURITY_GROUPS_USERS A
               LEFT JOIN "AspNetUsers" B              ON B."UserId" = A.USER_ID
               LEFT JOIN SM_SECURITY_GROUPS_MASTER C  ON C.ID = A.SECURITY_GROUP_ID
               LEFT JOIN FM_DIVISION D                ON D.ID = A.DIVISION_ID
        WHERE  A.IS_DELETED = 0
          AND  B."IsDeleted" = 0
          AND  A.SECURITY_GROUP_ID = :p_sg
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecurityGroupUser>> GetSecurityGroupUsersAsync(
        int securityGroupId, CancellationToken cancellationToken = default)
        => await QuerySqlAsync(
            SecurityGroupUsersSql,
            command => command.Parameters.Add(new OracleParameter("p_sg", OracleDbType.Int32) { Value = securityGroupId }),
            reader => new SecurityGroupUser(
                Int(reader, "USER_ID"),
                Str(reader, "USER_NAME")?.Trim(),
                Str(reader, "SG_NAME"),
                Str(reader, "DIVISION_NAME"),
                Int(reader, "SECURITY_GROUP_ID")),
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
            // Pending-for-approval sits one level BELOW the user's authorised level:
            // a doc awaiting the level-L approver is currently at APPROVE_LEVEL = L - 1.
            var pairs = table
                .Select(s => (Form: s.FormId, Level: s.LevelId - 1))
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
            // "Pending for approval" = actively awaiting a decision: submitted (1) or
            // reworked (2). Drafts/suspended (0) and terminal (3 reject / 4 approve) are excluded.
            case ApprovalDashboardFilter.Pending: where.Add("APPROVE_STATUS IN (1, 2)"); break;
            case ApprovalDashboardFilter.Approved: where.Add("APPROVE_STATUS = 4"); break;
            case ApprovalDashboardFilter.Rejected: where.Add("APPROVE_STATUS = 3"); break;
        }

        // Oracle's multi-column IN: the row is the user's only when BOTH the workflow
        // and the level match. The pairs are built as (FormId, LevelId - 1) in
        // GetUserDataAsync — a transaction awaiting the level-L approver currently sits
        // at APPROVE_LEVEL = L - 1. A null WORKFLOW_ID never matches, which is correct —
        // there is no workflow to authorise against.
        var tuples = string.Join(", ", pairs.Select((_, i) => $"(:w{i}, :l{i})"));
        where.Add($"(WORKFLOW_ID, APPROVE_LEVEL) IN ({tuples})");

        var sql =
            "SELECT ID, DOC_DATE, APPROVE_STATUS, APPROVE_LEVEL, WORKFLOW_ID, CREATED_BY, CREATED_AT, BRANCH_ID, COMP_ID " +
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
                        s?.MainDocType,
                        NullableInt(reader, "BRANCH_ID"),
                        NullableInt(reader, "COMP_ID")
                        );
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

    // --- Employee card (HRM_EMPLOYEE) ---

    // One employee with their contract's department and designation, plus the
    // profile-picture URL. The employee row carries PIC_NAME when a picture was
    // uploaded; otherwise the ERP falls back to "<contract id>.jpg" in the same
    // folder. Only active, non-deleted employees are returned. Ordered so that an
    // employee with several contract rows (renewals, transfers) resolves to the
    // current one — the highest contract id — and the caller takes the first row.
    private const string EmployeeSql = """
        SELECT A.ID                                  AS ID,
               A.USER_ID                             AS USER_ID,
               D.NAME                                AS DEPARTMENT_NAME,
               A.FULL_NAME                           AS EMPLOYEE,
               B.EMP_DEPARTMENT_ID                   AS EMP_DEPARTMENT_ID,
               B.EMP_DESIGNATION_ID                  AS EMP_DESIGNATION_ID,
               'https://erp.fakhruddin.ae:400/files/HR/Employees/E-'
                 || LPAD(A.ID, 5, '0') || '/'
                 || NVL(A.PIC_NAME, B.ID || '.jpg')  AS PROFILE
        FROM   HRM_EMPLOYEE A,
               HRM_EMPLOYEE_CONTRACT B,
               FM_DEPARTMENT D
        WHERE  A.ID = B.EMP_ID
          AND  B.EMP_DEPARTMENT_ID = D.ID
          AND  A.IS_DELETED = 0
          AND  A.IS_ACTIVE = 1
          AND  {0} = :p_id
        ORDER BY B.ID DESC
        """;

    /// <inheritdoc />
    public Task<ErpEmployee?> GetEmployeeDataByUserIdAsync(
        int userId, CancellationToken cancellationToken = default)
        => GetEmployeeAsync("A.USER_ID", userId, cancellationToken);

    /// <inheritdoc />
    public Task<ErpEmployee?> GetEmployeeDataByEmpIdAsync(
        int employeeId, CancellationToken cancellationToken = default)
        => GetEmployeeAsync("A.ID", employeeId, cancellationToken);

    // Both lookups are the same query differing only in the column matched, and that
    // column is a compile-time literal from the two callers above — never caller
    // input — so the format is safe; the id itself is always bound.
    private async Task<ErpEmployee?> GetEmployeeAsync(
        string column, int id, CancellationToken cancellationToken)
    {
        var rows = await QuerySqlAsync(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, EmployeeSql, column),
            command => command.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int32) { Value = id }),
            reader => new ErpEmployee(
                Int(reader, "ID"),
                Int(reader, "USER_ID"),
                Str(reader, "EMPLOYEE"),
                Str(reader, "DEPARTMENT_NAME"),
                Int(reader, "EMP_DEPARTMENT_ID"),
                Int(reader, "EMP_DESIGNATION_ID"),
                Str(reader, "PROFILE")),
            cancellationToken).ConfigureAwait(false);

        return rows.Count > 0 ? rows[0] : null;
    }

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
            "SELECT ID, DOC_DATE, APPROVE_STATUS, APPROVE_LEVEL, WORKFLOW_ID, CREATED_BY, CREATED_AT, BRANCH_ID, COMP_ID " +
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
                    menu.MainDocType,
                    NullableInt(reader, "BRANCH_ID"),
                    NullableInt(reader, "COMP_ID")),
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
