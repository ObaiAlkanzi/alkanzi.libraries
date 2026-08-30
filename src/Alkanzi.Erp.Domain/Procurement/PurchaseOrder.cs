using Alkanzi.Erp.Domain.Common;
using Alkanzi.Erp.Domain.Security;

namespace Alkanzi.Erp.Domain.Procurement;

/// <summary>A purchase order raised against a <see cref="Vendor"/>.</summary>
public class PurchaseOrder : AuditableEntity
{
    public int CompanyId { get; set; }

    /// <summary>Human-facing document number, unique per company among live rows.</summary>
    public int DocNum { get; set; }

    public DateOnly DocDate { get; set; }

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    /// <summary>Order total. Maps to <c>numeric</c> — never a float for money.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "AED";

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;

    /// <summary>Branch the document belongs to. Drives who may see it.</summary>
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string? Remarks { get; set; }
}
