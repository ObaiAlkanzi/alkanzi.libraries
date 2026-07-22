namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// What a document-type registry row has to expose for dispatch: the code
/// callers look it up by, the tenant it is configured for, and the table its
/// transactions live in.
/// </summary>
/// <remarks>
/// Implement this on your own entity — the ERP's <c>FM_TRANSACTION_MENU</c>, for
/// example — so <see cref="ApprovalEngine{TMenu}"/> can drive the lookup without
/// this package knowing the concrete type or its other columns.
/// </remarks>
public interface ITransactionMenu
{
    /// <summary>Document type code that callers dispatch on.</summary>
    /// <remarks>
    /// Not unique on its own: the same code is registered per company and
    /// branch, so it is only meaningful together with
    /// <see cref="ORG_ID"/>, <see cref="COMP_ID"/> and <see cref="BRANCH_ID"/>.
    /// </remarks>
    string DOC_TYPE { get; }

    /// <summary>Organisation this configuration belongs to.</summary>
    int ORG_ID { get; }

    /// <summary>Company this configuration belongs to.</summary>
    int COMP_ID { get; }

    /// <summary>Branch this configuration belongs to.</summary>
    int? BRANCH_ID { get; }

    /// <summary>
    /// Name of the table holding transactions of this document type, resolved
    /// to a CLR type through <see cref="IEntityResolver"/>.
    /// </summary>
    /// <remarks>
    /// Nullable, and commonly null: most registry rows configure numbering and
    /// workflow without naming a table. A null or blank value means the
    /// document type cannot be dispatched to a transaction row.
    /// </remarks>
    string? TABLE_NAME { get; }
}
