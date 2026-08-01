namespace Alkanzi.ErpServices;

/// <summary>
/// One approval row for a dashboard: the transaction's key fields plus the
/// document type's <c>DISPLAY_NAME</c> / <c>MAIN_DOC_TYPE</c> from the registry.
/// </summary>
public sealed record ApprovalDashboardRow(
    int Id,
    string DocType,
    DateTime? DocDate,
    int ApproveStatus,
    int ApproveLevel,
    int? WorkflowId,
    int CreatedBy,
    DateTime CreatedAt,
    string? DisplayName,
    string? MainDocType);

/// <summary>
/// Which approval rows a dashboard query returns, by <c>APPROVE_STATUS</c>.
/// </summary>
public enum ApprovalDashboardFilter
{
    /// <summary>Every row, whatever its status.</summary>
    All,

    /// <summary>Not yet decided — anything that is neither approved nor rejected (status 0/1/2).</summary>
    Pending,

    /// <summary>Approved (status 4).</summary>
    Approved,

    /// <summary>Rejected (status 3).</summary>
    Rejected,
}

/// <summary>
/// One row of the department-employee panel returned by
/// <c>PANEL.DEPARTMENT_EMPLOYEES</c>: <c>ID</c>, <c>USER_ID</c>,
/// <c>DEPARTMENT_NAME</c>, <c>EMPLOYEE</c>, <c>EMP_DEPARTMENT_ID</c>,
/// <c>EMP_DESIGNATION_ID</c>, <c>PROFILE</c>, <c>DESIGNATION</c> and
/// <c>STATUS</c>. Columns are mapped by name and tolerated when absent, so
/// <see cref="IsOnline"/> is simply <c>false</c> if a query omits <c>STATUS</c>.
/// </summary>
public sealed record DepartmentEmployee(
    int Id,
    int UserId,
    string? Employee,
    string? Profile,
    string? DepartmentName,
    int DepartmentId,
    int DesignationId,
    string? Designation,
    string? Status)
{
    // Statuses that count as "online" (present at work). STATUS is free ERP text
    // — "Absent", "Present", "On Annual leave", etc. — matched case-insensitively.
    // Add more values here if the ERP introduces them.
    private static readonly HashSet<string> OnlineStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Present", "Online" };

    /// <summary>True when <see cref="Status"/> means the employee is currently online.</summary>
    public bool IsOnline => IsOnlineStatus(Status);

    /// <summary>Whether a raw <c>STATUS</c> value counts as online.</summary>
    public static bool IsOnlineStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && OnlineStatuses.Contains(status.Trim());
}

/// <summary>
/// Dashboard reads over the ERP: approval rows across the document types a user
/// has access to, and the department-employee panel. For approvals, each doc type
/// is resolved to its transaction table through <c>FM_TRANSACTION_MENU</c>; a new
/// approvable table needs only to be mapped on <see cref="ErpDbContext"/>, no
/// dashboard change.
/// </summary>
public interface IErpApprovalDashboardService
{
    /// <summary>
    /// Returns approval rows for the given <paramref name="docTypes"/> — the menus
    /// the caller has access to. Each doc type is resolved to its table via the
    /// registry, read, and enriched with the menu's display name and main doc type.
    /// </summary>
    /// <param name="docTypes">The document types (menus) the caller may see.</param>
    /// <param name="filter">Which rows to return by status (default <see cref="ApprovalDashboardFilter.All"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApprovalDashboardRow>> GetDataAsync(
        IEnumerable<string> docTypes,
        ApprovalDashboardFilter filter = ApprovalDashboardFilter.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>PANEL.DEPARTMENT_EMPLOYEES(P_DEPARTMENT_ID)</c> for a department and
    /// maps its cursor to <see cref="DepartmentEmployee"/> rows, each flagged
    /// <see cref="DepartmentEmployee.IsOnline"/> by its status.
    /// </summary>
    /// <param name="departmentId">The department to list (the proc's <c>P_DEPARTMENT_ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<DepartmentEmployee>> GetDepartmentEmployeesAsync(
        int departmentId,
        CancellationToken cancellationToken = default);
}
