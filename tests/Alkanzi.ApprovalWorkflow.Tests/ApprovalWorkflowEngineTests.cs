using System;
using Alkanzi.ApprovalWorkflow;

namespace Alkanzi.ApprovalWorkflow.Tests
{
    public class ApprovalWorkflowEngineTests
    {
        private static ApprovalRequest ThreeLevelRequest() => new()
        {
            EntityType = "Budget",
            EntityId = "B-1001",
            Steps =
            {
                new ApprovalStep { Order = 1, ApproverRole = "LineManager" },
                new ApprovalStep { Order = 2, ApproverRole = "FinanceHead" },
                new ApprovalStep { Order = 3, ApproverRole = "CEO" },
            }
        };

        private readonly IApprovalWorkflowEngine _engine = new ApprovalWorkflowEngine();

        // ---- CurrentStep resolution ----

        [Fact]
        public void CurrentStep_OnFreshRequest_IsLowestOrderStep()
        {
            var request = ThreeLevelRequest();

            Assert.Equal(1, request.CurrentStep!.Order);
        }

        [Fact]
        public void CurrentStep_OrdersByOrderProperty_NotListPosition()
        {
            // Steps deliberately added out of order.
            var request = new ApprovalRequest
            {
                Steps =
                {
                    new ApprovalStep { Order = 3, ApproverRole = "CEO" },
                    new ApprovalStep { Order = 1, ApproverRole = "LineManager" },
                    new ApprovalStep { Order = 2, ApproverRole = "FinanceHead" },
                }
            };

            Assert.Equal("LineManager", request.CurrentStep!.ApproverRole);
        }

        [Fact]
        public void CurrentStep_WhenAllStepsActioned_IsNull()
        {
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-1");
            _engine.Approve(request, "u-2");
            _engine.Approve(request, "u-3");

            Assert.Null(request.CurrentStep);
        }

        [Fact]
        public void CurrentStep_OnRequestWithNoSteps_IsNull()
        {
            Assert.Null(new ApprovalRequest().CurrentStep);
        }

        // ---- Approve ----

        [Fact]
        public void Approve_StampsStatusApproverAndComment_OnCurrentStep()
        {
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-101", "Within Q3 allocation");

            var step = request.Steps[0];
            Assert.Equal(ApprovalStatus.Approved, step.Status);
            Assert.Equal("u-101", step.ApproverId);
            Assert.Equal("Within Q3 allocation", step.Comment);
            Assert.NotNull(step.ActionedAtUtc);
        }

        [Fact]
        public void Approve_WithoutComment_LeavesCommentNull()
        {
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-101");

            Assert.Null(request.Steps[0].Comment);
        }

        [Fact]
        public void Approve_StampsActionedAtUtcAsUtc()
        {
            var before = DateTime.UtcNow;
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-101");

            var actioned = request.Steps[0].ActionedAtUtc!.Value;
            Assert.InRange(actioned, before, DateTime.UtcNow);
        }

        [Fact]
        public void Approve_NonFinalStep_LeavesOverallStatusPending()
        {
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-101");

            Assert.Equal(ApprovalStatus.Pending, request.OverallStatus);
        }

        [Fact]
        public void Approve_AdvancesCurrentStepToNextLevel()
        {
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-101");

            Assert.Equal(2, request.CurrentStep!.Order);
            Assert.Equal("FinanceHead", request.CurrentStep.ApproverRole);
        }

        [Fact]
        public void Approve_AllSteps_SetsOverallStatusApproved()
        {
            var request = ThreeLevelRequest();

            _engine.Approve(request, "u-101");
            _engine.Approve(request, "u-204");
            _engine.Approve(request, "u-001");

            Assert.Equal(ApprovalStatus.Approved, request.OverallStatus);
            Assert.All(request.Steps, s => Assert.Equal(ApprovalStatus.Approved, s.Status));
        }

        [Fact]
        public void Approve_SingleStepChain_ImmediatelyApprovesOverall()
        {
            var request = new ApprovalRequest
            {
                Steps = { new ApprovalStep { Order = 1, ApproverRole = "Owner" } }
            };

            _engine.Approve(request, "u-1");

            Assert.Equal(ApprovalStatus.Approved, request.OverallStatus);
        }

        [Fact]
        public void Approve_ReturnsSameRequestInstance()
        {
            var request = ThreeLevelRequest();

            Assert.Same(request, _engine.Approve(request, "u-101"));
        }

        [Fact]
        public void Approve_WhenNoPendingStepRemains_Throws()
        {
            var request = ThreeLevelRequest();
            _engine.Approve(request, "u-1");
            _engine.Approve(request, "u-2");
            _engine.Approve(request, "u-3");

            Assert.Throws<InvalidOperationException>(() => _engine.Approve(request, "u-4"));
        }

        [Fact]
        public void Approve_OnRequestWithNoSteps_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => _engine.Approve(new ApprovalRequest(), "u-1"));
        }

        // ---- Reject ----

        [Fact]
        public void Reject_StampsStatusApproverAndComment_OnCurrentStep()
        {
            var request = ThreeLevelRequest();

            _engine.Reject(request, "u-204", "Exceeds department cap");

            var step = request.Steps[0];
            Assert.Equal(ApprovalStatus.Rejected, step.Status);
            Assert.Equal("u-204", step.ApproverId);
            Assert.Equal("Exceeds department cap", step.Comment);
            Assert.NotNull(step.ActionedAtUtc);
        }

        [Fact]
        public void Reject_AtAnyLevel_SetsOverallStatusRejected()
        {
            var request = ThreeLevelRequest();
            _engine.Approve(request, "u-101");

            _engine.Reject(request, "u-204", "Exceeds department cap");

            Assert.Equal(ApprovalStatus.Rejected, request.OverallStatus);
        }

        [Fact]
        public void Reject_LeavesRemainingStepsUntouched()
        {
            var request = ThreeLevelRequest();

            _engine.Reject(request, "u-101", "No budget");

            Assert.Equal(ApprovalStatus.Pending, request.Steps[1].Status);
            Assert.Equal(ApprovalStatus.Pending, request.Steps[2].Status);
            Assert.Null(request.Steps[1].ApproverId);
        }

        [Fact]
        public void Reject_ReturnsSameRequestInstance()
        {
            var request = ThreeLevelRequest();

            Assert.Same(request, _engine.Reject(request, "u-101"));
        }

        [Fact]
        public void Reject_WhenNoPendingStepRemains_Throws()
        {
            var request = new ApprovalRequest
            {
                Steps = { new ApprovalStep { Order = 1, ApproverRole = "Owner" } }
            };
            _engine.Approve(request, "u-1");

            Assert.Throws<InvalidOperationException>(() => _engine.Reject(request, "u-2"));
        }

        // ---- Documented current behaviour (see README note) ----

        [Fact]
        public void Approve_AfterReject_CurrentlyResumesWorkflow()
        {
            // A rejected step is no longer Pending, so CurrentStep advances past it
            // and a subsequent Approve resets OverallStatus away from Rejected.
            // This test pins the CURRENT behaviour, not necessarily the desired one.
            var request = ThreeLevelRequest();
            _engine.Reject(request, "u-101", "No budget");

            _engine.Approve(request, "u-204");

            Assert.Equal(ApprovalStatus.Pending, request.OverallStatus);
        }
    }
}
