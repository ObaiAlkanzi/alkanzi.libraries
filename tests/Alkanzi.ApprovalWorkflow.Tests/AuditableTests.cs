using System;
using Alkanzi.ApprovalWorkflow;
using Alkanzi.Auditable;

namespace Alkanzi.ApprovalWorkflow.Tests
{
    public class AuditableTests
    {
        // IAuditable stamps UTC. Convert to a display timezone at the UI layer
        // rather than storing local time — see the Alkanzi.Auditable README.

        [Fact]
        public void MarkCreated_StampsCreatorAndClearsFlags()
        {
            IAuditable entity = new ApprovalRequest { IS_UPDATED = true, IS_DELETED = true };
            var before = DateTime.UtcNow;

            entity.MarkCreated(42);

            Assert.Equal(42, entity.CREATED_BY);
            Assert.InRange(entity.CREATED_AT, before, DateTime.UtcNow);
            Assert.Equal(false, entity.IS_UPDATED);
            Assert.Equal(false, entity.IS_DELETED);
        }

        [Fact]
        public void MarkUpdated_StampsUpdaterAndSetsFlag()
        {
            IAuditable entity = new ApprovalRequest();
            var before = DateTime.UtcNow;

            entity.MarkUpdated(7);

            Assert.Equal(true, entity.IS_UPDATED);
            Assert.Equal(7, entity.UPDATED_BY);
            Assert.InRange(entity.UPDATED_AT!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public void MarkDeleted_StampsDeleterAndSetsFlag()
        {
            IAuditable entity = new ApprovalRequest();
            var before = DateTime.UtcNow;

            entity.MarkDeleted(3);

            Assert.Equal(true, entity.IS_DELETED);
            Assert.Equal(3, entity.DELETED_BY);
            Assert.InRange(entity.DELETED_AT!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public void MarkDeleted_DoesNotClearCreationStamp()
        {
            IAuditable entity = new ApprovalRequest();
            entity.MarkCreated(42);

            entity.MarkDeleted(3);

            Assert.Equal(42, entity.CREATED_BY);
        }

        [Fact]
        public void MarkUpdated_DoesNotSetDeletedFlag()
        {
            IAuditable entity = new ApprovalRequest();

            entity.MarkUpdated(7);

            Assert.Null(entity.IS_DELETED);
            Assert.Null(entity.DELETED_BY);
        }

        [Fact]
        public void NewRequest_HasNoAuditStampsUntilMarked()
        {
            IAuditable entity = new ApprovalRequest();

            Assert.Equal(0, entity.CREATED_BY);
            Assert.Null(entity.UPDATED_BY);
            Assert.Null(entity.DELETED_BY);
            Assert.Null(entity.UPDATED_AT);
            Assert.Null(entity.DELETED_AT);
            Assert.Null(entity.IS_UPDATED);
            Assert.Null(entity.IS_DELETED);
        }

        [Fact]
        public void ApprovalRequest_Defaults_AreSensible()
        {
            var request = new ApprovalRequest();

            Assert.False(string.IsNullOrWhiteSpace(request.Id));
            Assert.Equal(ApprovalStatus.Pending, request.OverallStatus);
            Assert.Empty(request.Steps);
            Assert.Equal(string.Empty, request.EntityType);
        }

        [Fact]
        public void ApprovalRequest_IdsAreUniquePerInstance()
        {
            Assert.NotEqual(new ApprovalRequest().Id, new ApprovalRequest().Id);
        }
    }
}
