using Alkanzi.Erp.Domain.Common;

namespace Alkanzi.Erp.Domain.Procurement;

/// <summary>A supplier the company buys from.</summary>
public class Vendor : AuditableEntity
{
    public int CompanyId { get; set; }

    public string Name { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>Tax registration number (UAE TRN).</summary>
    public string? Trn { get; set; }

    public int BranchId { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
