namespace Alkanzi.SearchEngine;

/// <summary>
/// The seam that lets the generic <see cref="TransactionSearchProvider{T}"/> work over any
/// ERP transaction entity. The member names/types deliberately mirror
/// <c>Modules_DataTables.TRANSACTION_BASE</c>, so once that base declares
/// <c>: ISearchableTransaction</c> (it already has every property) all inheriting
/// entities become searchable with zero extra code.
/// </summary>
public interface ISearchableTransaction
{
    int ID { get; }
    int DOC_NUM { get; }
    string DOC_TYPE { get; }
    DateTime DOC_DATE { get; }
    int BRANCH_ID { get; }
    int DOC_STATUS { get; }
    int APPROVE_STATUS { get; }
    bool IS_DELETED { get; }
}
