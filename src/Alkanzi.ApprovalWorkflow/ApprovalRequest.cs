
using Alkanzi.Auditable;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alkanzi.ApprovalWorkflow
{
    public class ApprovalRequest: IAuditable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EntityType { get; set; } = string.Empty; // e.g. "Budget", "LeaseContract", "LegalCase"
        public string EntityId { get; set; } = string.Empty;
        public List<ApprovalStep> Steps { get; set; } = new();
        public ApprovalStatus OverallStatus { get; set; } = ApprovalStatus.Pending;

        public ApprovalStep? CurrentStep =>
            Steps.OrderBy(s => s.Order)
                 .FirstOrDefault(s => s.Status == ApprovalStatus.Pending);

        // IAuditable implementation
        public bool? IS_UPDATED { get; set; }
        public bool? IS_DELETED { get; set; }
        public int CREATED_BY { get; set; }
        public int? UPDATED_BY { get; set; }
        public int? DELETED_BY { get; set; }
        public DateTime CREATED_AT { get; set; }
        public DateTime? UPDATED_AT { get; set; }
        public DateTime? DELETED_AT { get; set; }
    }
}
