using Alkanzi.Auditable;

namespace Alkanzi.Erp.Data.Entities;

/// <summary>Approval state of a document. Mirrors the ERP's APPROVE_STATUS convention.</summary>
public enum ApprovalStatus
{
    Draft = 0,
    Pending = 1,
    Rejected = 3,
    Approved = 4,
}

/// <summary>A purchase order raised against a <see cref="Entities.Vendor"/>.</summary>
public class PurchaseOrder : IAuditable
{
    public int Id { get; set; }

    /// <summary>Human-facing document number, unique per company.</summary>
    public int DocNum { get; set; }

    public DateOnly DocDate { get; set; }

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    /// <summary>Order total. <c>numeric</c> in PostgreSQL — never a float for money.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "AED";

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;

    public int BranchId { get; set; }

    public string? Remarks { get; set; }

    // ---- IAuditable ----
    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}
