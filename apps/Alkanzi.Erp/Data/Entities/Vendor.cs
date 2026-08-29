using Alkanzi.Auditable;

namespace Alkanzi.Erp.Data.Entities;

/// <summary>
/// A supplier the company buys from.
/// <para>
/// Implements <see cref="IAuditable"/>, so the audit columns are stamped by the interceptor
/// and a delete becomes a soft delete — nothing in this class has to do that itself.
/// </para>
/// </summary>
public class Vendor : IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>Tax registration number (UAE TRN).</summary>
    public string? Trn { get; set; }

    public int BranchId { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

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
