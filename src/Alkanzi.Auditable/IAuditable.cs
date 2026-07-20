using System;
using System.Collections.Generic;
using System.Text;

namespace Alkanzi.Auditable
{
    public interface IAuditable
    {
        bool? IS_UPDATED { get; set; }
        bool? IS_DELETED { get; set; }
        int CREATED_BY { get; set; }
        int? UPDATED_BY { get; set; }
        int? DELETED_BY { get; set; }
        DateTime CREATED_AT { get; set; }
        DateTime? UPDATED_AT { get; set; }
        DateTime? DELETED_AT { get; set; }

        // Default interface methods — real logic lives here now
        void MarkCreated(int userId)
        {
            CREATED_BY = userId;
            CREATED_AT = DateTime.UtcNow;
            IS_UPDATED = false;
            IS_DELETED = false;
        }

        void MarkUpdated(int userId)
        {
            IS_UPDATED = true;
            UPDATED_BY = userId;
            UPDATED_AT = DateTime.UtcNow;
        }

        void MarkDeleted(int userId)
        {
            IS_DELETED = true;
            DELETED_BY = userId;
            DELETED_AT = DateTime.UtcNow;
        }
    }
}
