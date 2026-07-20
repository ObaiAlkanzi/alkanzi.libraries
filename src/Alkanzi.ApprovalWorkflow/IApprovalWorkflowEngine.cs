using System;
using System.Collections.Generic;
using System.Text;

namespace Alkanzi.ApprovalWorkflow
{
    public interface IApprovalWorkflowEngine
    {
        /// <summary>
     /// Approves the current pending step of the request.
     /// </summary>
        ApprovalRequest Approve(ApprovalRequest request, string approverId, string? comment = null);

        /// <summary>
        /// Rejects the current pending step, halting the workflow.
        /// </summary>
        ApprovalRequest Reject(ApprovalRequest request, string approverId, string? comment = null);
    }
}
