using Alkanzi.Auditable;

namespace Alkanzi.Erp.Domain.Common;

/// <summary>
/// Base for every persisted entity that carries audit columns.
/// <para>
/// The properties are the contract from <see cref="IAuditable"/>, so the
/// Alkanzi.Auditable interceptor stamps them and rewrites deletes into soft deletes —
/// nothing in the domain has to remember to do either. The interceptor emits no SQL, so
/// depending on this here costs the domain no infrastructure.
/// </para>
/// </summary>
public abstract class AuditableEntity : IAuditable
{
    public int Id { get; set; }

    public bool? IS_UPDATED { get; set; }
    public bool? IS_DELETED { get; set; }
    public int CREATED_BY { get; set; }
    public int? UPDATED_BY { get; set; }
    public int? DELETED_BY { get; set; }
    public DateTime CREATED_AT { get; set; }
    public DateTime? UPDATED_AT { get; set; }
    public DateTime? DELETED_AT { get; set; }
}
