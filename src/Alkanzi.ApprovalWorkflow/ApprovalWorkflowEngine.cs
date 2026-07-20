using System;
using System.Collections.Generic;
using System.Text;

namespace Alkanzi.ApprovalWorkflow
{
    public class ApprovalWorkflowEngine : IApprovalWorkflowEngine
    {
        public ApprovalRequest Approve(ApprovalRequest request, string approverId, string? comment = null)
        {
            var step = request.CurrentStep
                ?? throw new InvalidOperationException("No pending step to approve.");
            step.Status = ApprovalStatus.Approved;
            step.ApproverId = approverId;
            step.Comment = comment;
            step.ActionedAtUtc = DateTime.UtcNow;

            request.OverallStatus = request.Steps.All(s => s.Status == ApprovalStatus.Approved)
                ? ApprovalStatus.Approved
                : ApprovalStatus.Pending;

            return request;
        }

        public ApprovalRequest Reject(ApprovalRequest request, string approverId, string? comment = null)
        {
            var step = request.CurrentStep
                ?? throw new InvalidOperationException("No pending step to approve.");

            step.Status = ApprovalStatus.Rejected;
            step.ApproverId = approverId;
            step.Comment = comment;
            step.ActionedAtUtc = DateTime.UtcNow;

            request.OverallStatus = ApprovalStatus.Rejected;

            return request;
        }
    }
}
