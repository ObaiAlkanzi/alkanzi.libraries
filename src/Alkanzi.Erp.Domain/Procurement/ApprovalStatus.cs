namespace Alkanzi.Erp.Domain.Procurement;

/// <summary>
/// Approval state of a document. The numbering is the ERP's existing <c>APPROVE_STATUS</c>
/// convention, kept identical so data moving between the two systems needs no translation.
/// </summary>
public enum ApprovalStatus
{
    Draft = 0,
    Pending = 1,
    Rejected = 3,
    Approved = 4,
}
