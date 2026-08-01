using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
public sealed class ErpApprovalDashboardService : IErpApprovalDashboardService
{
    private static readonly MethodInfo QueryTableMethod = typeof(ErpApprovalDashboardService)
        .GetMethod(nameof(QueryTableAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly ErpDbContext _context;
    private readonly IErpProcedureService _procedures;

    /// <summary>
    /// Creates the service over the ERP context. The procedure runner (used by the
    /// department-employee panel) is optional and self-provisions over the same
    /// context when not supplied.
    /// </summary>
    public ErpApprovalDashboardService(ErpDbContext context, IErpProcedureService? procedures = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _procedures = procedures ?? new ErpProcedureService(context);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalDashboardRow>> GetDataAsync(
        IEnumerable<string> docTypes,
        ApprovalDashboardFilter filter = ApprovalDashboardFilter.All,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(docTypes);

        var types = docTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (types.Count == 0)
        {
            return [];
        }

        // One query for the accessible menus — TABLE_NAME to dispatch on, plus the
        // DISPLAY_NAME / MAIN_DOC_TYPE the rows are enriched with.
        var menus = await _context.TransactionMenus
            .Where(m => types.Contains(m.DOC_TYPE))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Resolve every accessible menu to its approvable CLR type first. Skip a
        // doc type whose table is unmapped or not approvable (e.g. the approval-log
        // tables, which are IErpAuditable but not IErpApprovable).
        var resolved = new List<(FM_TRANSACTION_MENU Menu, Type ClrType)>();
        foreach (var menu in menus)
        {
            if (string.IsNullOrWhiteSpace(menu.TABLE_NAME))
            {
                continue;
            }

            var clrType = ResolveClrType(menu.TABLE_NAME);
            if (clrType is null || !typeof(IErpApprovable).IsAssignableFrom(clrType))
            {
                continue;
            }

            resolved.Add((menu, clrType));
        }

        // A table is only filtered by DOC_TYPE when it is shared by more than one
        // doc type. Decide that from the full registry — not just the menus this
        // caller can see — so a shared table the caller has partial access to is
        // still filtered (otherwise it would leak the other doc types' rows), and
        // a single-doc-type table (whose physical DOC_TYPE column may not even
        // exist) is never filtered — filtering it would throw ORA-00904.
        var sharedTables = await ResolveSharedTablesAsync(resolved, cancellationToken).ConfigureAwait(false);

        var rows = new List<ApprovalDashboardRow>();
        foreach (var (menu, clrType) in resolved)
        {
            var applyDocTypeFilter = sharedTables.Contains(clrType);

            var task = (Task<List<ApprovalDashboardRow>>)QueryTableMethod
                .MakeGenericMethod(clrType)
                .Invoke(this, [menu, filter, applyDocTypeFilter, cancellationToken])!;

            rows.AddRange(await task.ConfigureAwait(false));
        }

        return rows;
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

    // Generic per-table query, invoked by reflection for the resolved CLR type. ID,
    // CREATED_BY and CREATED_AT are not on IErpApprovable, so they are read through
    // EF.Property (mapped to their columns); the menu fields are projected as
    // constants. When the table is shared by several doc types it is filtered by
    // DOC_TYPE so only this one's rows return; a single-doc-type table is not
    // filtered (its physical DOC_TYPE column may not even exist).
    private async Task<List<ApprovalDashboardRow>> QueryTableAsync<TEntity>(
        FM_TRANSACTION_MENU menu, ApprovalDashboardFilter filter, bool applyDocTypeFilter, CancellationToken cancellationToken)
        where TEntity : class, IErpApprovable
    {
        const int rejected = (int)ApprovalAction.Reject;   // 3
        const int approved = (int)ApprovalAction.Approve;  // 4

        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (applyDocTypeFilter)
        {
            query = query.Where(e => e.DOC_TYPE == menu.DOC_TYPE);
        }

        // Pending = anything not yet terminal (0/1/2); Approved = 4; Rejected = 3.
        query = filter switch
        {
            ApprovalDashboardFilter.Pending => query.Where(e => e.APPROVE_STATUS != rejected && e.APPROVE_STATUS != approved),
            ApprovalDashboardFilter.Approved => query.Where(e => e.APPROVE_STATUS == approved),
            ApprovalDashboardFilter.Rejected => query.Where(e => e.APPROVE_STATUS == rejected),
            _ => query,
        };

        return await query
            .Select(e => new ApprovalDashboardRow(
                EF.Property<int>(e, "ID"),
                menu.DOC_TYPE,
                (DateTime?)e.DOC_DATE,
                e.APPROVE_STATUS,
                e.APPROVE_LEVEL,
                e.WORKFLOW_ID,
                EF.Property<int>(e, "CREATED_BY"),
                EF.Property<DateTime>(e, "CREATED_AT"),
                menu.DISPLAY_NAME,
                menu.MAIN_DOC_TYPE))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    // The subset of the resolved CLR types whose table backs more than one doc
    // type in the registry — the only ones that need (and can have) a DOC_TYPE
    // filter. Counts distinct doc types per table across the WHOLE registry, so a
    // caller with partial access to a shared table still filters it.
    private async Task<HashSet<Type>> ResolveSharedTablesAsync(
        IReadOnlyCollection<(FM_TRANSACTION_MENU Menu, Type ClrType)> resolved,
        CancellationToken cancellationToken)
    {
        var tableNames = resolved
            .Select(r => r.Menu.TABLE_NAME!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (tableNames.Count == 0)
        {
            return [];
        }

        // Every registry row on those tables — including doc types this caller
        // cannot see — reduced to a distinct-doc-type count per table.
        var registry = await _context.TransactionMenus
            .Where(m => m.TABLE_NAME != null && tableNames.Contains(m.TABLE_NAME.ToUpper()))
            .Select(m => new { m.TABLE_NAME, m.DOC_TYPE })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var docTypesByTable = registry
            .GroupBy(x => x.TABLE_NAME!.Trim().ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.DOC_TYPE).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        return resolved
            .Where(r => docTypesByTable.TryGetValue(r.Menu.TABLE_NAME!.Trim().ToUpperInvariant(), out var count) && count > 1)
            .Select(r => r.ClrType)
            .ToHashSet();
    }

    // Maps a table name to the CLR type mapped to it, through EF's model — the same
    // resolution the approval engine uses.
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
