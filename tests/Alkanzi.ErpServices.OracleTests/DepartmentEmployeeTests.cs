namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Status → online mapping for <see cref="DepartmentEmployee"/>. Pure logic, no
/// Oracle needed (the live proc is exercised in
/// <see cref="ErpApprovalDashboardOracleTests"/>).
/// </summary>
public class DepartmentEmployeeTests
{
    [Theory]
    [InlineData("Present", true)]
    [InlineData("present", true)]     // case-insensitive
    [InlineData("  Present  ", true)] // trimmed
    [InlineData("Online", true)]
    [InlineData("Absent", false)]
    [InlineData("On Annual leave", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsOnline_reflects_status(string? status, bool expectedOnline)
    {
        var employee = new DepartmentEmployee(
            Id: 1, UserId: 2, Employee: "Jane", Profile: "jane.png",
            DepartmentName: "Finance", DepartmentId: 3, DesignationId: 4,
            Designation: "Manager", Status: status);

        Assert.Equal(expectedOnline, employee.IsOnline);
        Assert.Equal(expectedOnline, DepartmentEmployee.IsOnlineStatus(status));
    }
}
