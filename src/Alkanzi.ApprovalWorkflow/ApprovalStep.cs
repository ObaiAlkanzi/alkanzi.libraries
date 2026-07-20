using System;
using System.Collections.Generic;
using System.Text;

namespace Alkanzi.ApprovalWorkflow
{
    public class ApprovalStep
    {
        public int Order { get; set; }
        public string ApproverRole { get; set; } = string.Empty;
        public string? ApproverId { get; set; }
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public string? Comment { get; set; }
        public DateTime? ActionedAtUtc { get; set; }
    }
    
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }
}
