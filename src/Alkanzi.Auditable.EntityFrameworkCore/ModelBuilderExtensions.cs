using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <summary>
/// Model-building helpers that keep soft-deleted rows out of ordinary queries.
/// </summary>
public static class ModelBuilderExtensions
{
    private const string IsDeleted = nameof(IAuditable.IS_DELETED);

    /// <summary>
    /// Adds a global query filter — <c>IS_DELETED != true</c> — to every mapped
    /// <see cref="IAuditable"/> entity type, so soft-deleted rows disappear from
    /// queries unless explicitly asked for with <c>IgnoreQueryFilters()</c>.
    /// </summary>
    /// <param name="modelBuilder">The model builder, from <c>OnModelCreating</c>.</param>
    /// <param name="shouldApply">
    /// Optional predicate to opt individual CLR types out of filtering.
    /// Return <see langword="false"/> to leave a type unfiltered.
    /// </param>
    /// <returns>The same <paramref name="modelBuilder"/>, for chaining.</returns>
    /// <remarks>
    /// Call this at the <em>end</em> of <c>OnModelCreating</c>, after your own
    /// configuration. Entity types that already declare a query filter are left
    /// alone rather than overwritten — add <c>IS_DELETED != true</c> to those
    /// filters yourself. Owned types and derived types are skipped, because EF
    /// Core only accepts filters on root entity types; a filtered root already
    /// covers both.
    /// </remarks>
    public static ModelBuilder ApplyAuditableQueryFilters(
        this ModelBuilder modelBuilder,
        Func<Type, bool>? shouldApply = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IAuditable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            if (entityType.BaseType is not null || entityType.IsOwned())
            {
                continue;
            }

            if (shouldApply is not null && !shouldApply(entityType.ClrType))
            {
                continue;
            }

            if (HasExistingQueryFilter(entityType))
            {
                continue;
            }

            var filter = BuildNotDeletedFilter(entityType);
            if (filter is not null)
            {
                entityType.SetQueryFilter(filter);
            }
        }

        return modelBuilder;
    }

    private static bool HasExistingQueryFilter(IMutableEntityType entityType)
#pragma warning disable EF9100, CS0618 // Named query filters (EF 10) supersede this; the single-filter read is what we need and still works.
        => entityType.GetQueryFilter() is not null;
#pragma warning restore EF9100, CS0618

    /// <summary>
    /// Builds <c>e =&gt; e.IS_DELETED != true</c> for the given entity type.
    /// Returns <see langword="null"/> when <c>IS_DELETED</c> is not mapped —
    /// an entity may implement <see cref="IAuditable"/> while ignoring the
    /// column, and filtering on an unmapped property would fail at runtime.
    /// </summary>
    private static LambdaExpression? BuildNotDeletedFilter(IMutableEntityType entityType)
    {
        var property = entityType.FindProperty(IsDeleted);
        if (property is null)
        {
            return null;
        }

        var parameter = Expression.Parameter(entityType.ClrType, "e");

        // A shadow property, or one behind an explicit interface implementation,
        // has no usable PropertyInfo — reach it through EF.Property instead.
        Expression access = property.PropertyInfo is { } propertyInfo
            ? Expression.Property(parameter, propertyInfo)
            : Expression.Call(
                EfPropertyMethod.MakeGenericMethod(typeof(bool?)),
                parameter,
                Expression.Constant(IsDeleted));

        // != true rather than == false: IS_DELETED is bool?, and a never-deleted
        // row may legitimately hold null. EF expands this to the SQL null check.
        var body = Expression.NotEqual(access, Expression.Constant(true, typeof(bool?)));

        return Expression.Lambda(body, parameter);
    }

    private static readonly MethodInfo EfPropertyMethod =
        typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException("EF.Property<TProperty> could not be located.");
}
