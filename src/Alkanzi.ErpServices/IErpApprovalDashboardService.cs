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
    string? MainDocType,
    int? BranchId = null,
    int? CompId = null);

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
/// One (workflow form, level) a user may act at, resolved from their security
/// groups — the unit of "what can this user approve".
/// </summary>
/// <remarks>
/// A user reaches a level through <c>SM_DIVISION_SECURITY_GROUPS_USERS</c> →
/// <c>SM_WORKFLOW_LVL_SECURITY_GROUPS</c> → <c>SM_WORKFLOW_FORMS</c>. The same
/// (form, level) can arrive through several security groups; those are collapsed.
/// </remarks>
/// <param name="FormId">The workflow form (<c>SM_WORKFLOW_FORMS.ID</c>) — matches a transaction's <c>WORKFLOW_ID</c>.</param>
/// <param name="LevelId">The level in that form the user may act at — matches a transaction's <c>APPROVE_LEVEL</c>.</param>
/// <param name="WorkflowName">The form's name, e.g. "Inventory LPO - IT".</param>
/// <param name="LastLevel">The form's final level.</param>
/// <param name="TableName">The transaction table the form's document type lives in.</param>
/// <param name="DocType">The document type.</param>
/// <param name="DisplayName">The document type's display name.</param>
/// <param name="MainDocType">The parent/main document type, or null.</param>
public sealed record UserApprovalScope(
    int FormId,
    int LevelId,
    string? WorkflowName,
    int LastLevel,
    string? TableName,
    string DocType,
    string? DisplayName,
    string? MainDocType);

/// <summary>
/// One security group a user belongs to — its id and name, from
/// <c>SM_DIVISION_SECURITY_GROUPS_USERS</c> joined to <c>SM_SECURITY_GROUPS_MASTER</c>.
/// </summary>
/// <param name="SecurityGroupId">The security group id (<c>SECURITY_GROUP_ID</c>).</param>
/// <param name="Name">The group's name, or null when the master row is missing (LEFT JOIN).</param>
public sealed record UserSecurityGroup(
    int SecurityGroupId,
    string? Name);

/// <summary>
/// One workflow form a security group sits on, from
/// <c>SM_WORKFLOW_LVL_SECURITY_GROUPS</c> joined to <c>SM_WORKFLOW_FORMS</c>.
/// </summary>
/// <remarks>
/// A group can appear on several levels of the same form; those are collapsed, so
/// each form is returned once. Use <see cref="IErpApprovalDashboardService.GetUserScopeAsync"/>
/// when the level matters.
/// </remarks>
/// <param name="WorkflowId">The workflow form (<c>SM_WORKFLOW_FORMS.ID</c>) — matches a transaction's <c>WORKFLOW_ID</c>.</param>
/// <param name="Name">The form's name, e.g. "Inventory LPO - IT".</param>
/// <param name="SecurityGroupId">The security group the form was reached through.</param>
public sealed record SecurityGroupWorkflow(
    int WorkflowId,
    string? Name,
    int SecurityGroupId);

/// <summary>
/// One member of a security group, from <c>SM_DIVISION_SECURITY_GROUPS_USERS</c>
/// joined to <c>AspNetUsers</c>, <c>SM_SECURITY_GROUPS_MASTER</c> and
/// <c>FM_DIVISION</c>.
/// </summary>
/// <remarks>
/// Membership is per division, so a user who belongs to the same group under
/// several divisions is returned once per division — the rows differ by
/// <paramref name="DivisionName" />.
/// </remarks>
/// <param name="UserId">The login user (<c>AspNetUsers."UserId"</c>).</param>
/// <param name="UserName">The user's full name — first and last name joined.</param>
/// <param name="SecurityGroupName">The group's name, or null when the master row is missing (LEFT JOIN).</param>
/// <param name="DivisionName">The division the membership is under, or null when the division row is missing (LEFT JOIN).</param>
/// <param name="SecurityGroupId">The security group the user was reached through.</param>
public sealed record SecurityGroupUser(
    int UserId,
    string? UserName,
    string? SecurityGroupName,
    string? DivisionName,
    int SecurityGroupId);

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
/// One employee's identity card: who they are, which department and designation
/// their contract puts them in, and the URL of their profile picture. Read from
/// <c>HRM_EMPLOYEE</c> joined to <c>HRM_EMPLOYEE_CONTRACT</c> and
/// <c>FM_DEPARTMENT</c>; only active, non-deleted employees are returned.
/// </summary>
/// <param name="Id">The employee id (<c>HRM_EMPLOYEE.ID</c>).</param>
/// <param name="UserId">The login user the employee is tied to (<c>HRM_EMPLOYEE.USER_ID</c>).</param>
/// <param name="Employee">The employee's full name.</param>
/// <param name="DepartmentName">The contract department's name (<c>FM_DEPARTMENT.NAME</c>).</param>
/// <param name="DepartmentId">The contract's <c>EMP_DEPARTMENT_ID</c>.</param>
/// <param name="DesignationId">The contract's <c>EMP_DESIGNATION_ID</c>.</param>
/// <param name="Profile">Absolute URL of the profile picture, built from the employee id and <c>PIC_NAME</c>.</param>
public sealed record ErpEmployee(
    int Id,
    int UserId,
    string? Employee,
    string? DepartmentName,
    int DepartmentId,
    int DesignationId,
    string? Profile);

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
    /// The (workflow form, level) pairs a user may act at, resolved from their
    /// security groups. Use it to build a menu, or to see why a transaction is or
    /// is not on someone's list.
    /// </summary>
    /// <param name="userId">The user (<c>SM_DIVISION_SECURITY_GROUPS_USERS.USER_ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<UserApprovalScope>> GetUserScopeAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the transactions actually waiting on a user — the rows sitting at a
    /// level that user's security groups authorise, in the workflow they authorise
    /// it for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matching is on <b>(<c>WORKFLOW_ID</c>, <c>APPROVE_LEVEL</c>)</b>, not on
    /// document type. Document type alone is wrong twice over: one table serves many
    /// doc types (<c>PRF_TRANSACTIONS</c>, <c>FM_RECEIPTS_MASTER</c>), and one doc
    /// type runs under several workflow forms with different levels — so a doc-type
    /// filter returns transactions the user cannot act on. <c>APPROVE_LEVEL</c> is
    /// the level authorisation is evaluated at, which is what
    /// <see cref="IErpApprovalEngine.ApplyApprovalAsync"/> passes to
    /// <c>APPROVAL_REVERT_PAK.LVL_AUTHORIZATION</c>.
    /// </para>
    /// <para>
    /// Rows with a null <c>WORKFLOW_ID</c> are never returned: with no workflow there
    /// is no level to authorise against.
    /// </para>
    /// </remarks>
    /// <param name="userId">The user (<c>SM_DIVISION_SECURITY_GROUPS_USERS.USER_ID</c>).</param>
    /// <param name="filter">Which rows to return by status (default <see cref="ApprovalDashboardFilter.Pending"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ApprovalDashboardRow>> GetUserDataAsync(
        int userId,
        ApprovalDashboardFilter filter = ApprovalDashboardFilter.Pending,
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

    /// <summary>
    /// The security groups a user belongs to (id + name), from
    /// <c>SM_DIVISION_SECURITY_GROUPS_USERS</c> joined to the group master. A group
    /// reached through several division rows is returned once.
    /// </summary>
    /// <param name="userId">The user (<c>SM_DIVISION_SECURITY_GROUPS_USERS.USER_ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<UserSecurityGroup>> GetUserSecurityGroupsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The workflow forms a security group is on, from
    /// <c>SM_WORKFLOW_LVL_SECURITY_GROUPS</c> joined to <c>SM_WORKFLOW_FORMS</c>.
    /// A group on several levels of the same form yields that form once.
    /// </summary>
    /// <param name="securityGroupId">The security group (<c>SM_WORKFLOW_LVL_SECURITY_GROUPS.SECURITY_GROUP_ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SecurityGroupWorkflow>> GetSecurityGroupWorkflowsAsync(
        int securityGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The users in a security group, with the group and division names, from
    /// <c>SM_DIVISION_SECURITY_GROUPS_USERS</c> joined to <c>AspNetUsers</c>,
    /// <c>SM_SECURITY_GROUPS_MASTER</c> and <c>FM_DIVISION</c>. Deleted membership
    /// rows and deleted users are excluded; a user in the group under several
    /// divisions is returned once per division.
    /// </summary>
    /// <param name="securityGroupId">The security group (<c>SM_DIVISION_SECURITY_GROUPS_USERS.SECURITY_GROUP_ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SecurityGroupUser>> GetSecurityGroupUsersAsync(
        int securityGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The employee behind a login user — <c>HRM_EMPLOYEE.USER_ID</c> lookup.
    /// Returns <c>null</c> when the user has no active employee record (or none
    /// with a contract that resolves to a department).
    /// </summary>
    /// <remarks>
    /// An employee with several contract rows (renewals, transfers) resolves to the
    /// most recent one — the highest <c>HRM_EMPLOYEE_CONTRACT.ID</c> — so the
    /// department and designation returned are the current ones.
    /// </remarks>
    /// <param name="userId">The login user (<c>HRM_EMPLOYEE.USER_ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ErpEmployee?> GetEmployeeDataByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The same employee card as <see cref="GetEmployeeDataByUserIdAsync"/>, looked
    /// up by employee id (<c>HRM_EMPLOYEE.ID</c>) instead of login user. Returns
    /// <c>null</c> when there is no such active employee.
    /// </summary>
    /// <param name="employeeId">The employee (<c>HRM_EMPLOYEE.ID</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ErpEmployee?> GetEmployeeDataByEmpIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default);
}
