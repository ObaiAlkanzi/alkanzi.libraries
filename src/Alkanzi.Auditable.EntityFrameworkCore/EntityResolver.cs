using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Alkanzi.Auditable.EntityFrameworkCore;

/// <inheritdoc cref="IEntityResolver" />
public sealed class EntityResolver : IEntityResolver
{
    // Keyed on the model, not the context: EF caches one model per context type
    // and it is immutable, so the lookup is built once per application rather
    // than once per request. The weak table lets it die with the model.
    private static readonly ConditionalWeakTable<IModel, IReadOnlyDictionary<string, IEntityType[]>> LookupCache = new();

    private readonly DbContext _context;

    /// <summary>Creates a resolver over the given context's model.</summary>
    public EntityResolver(DbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public IEntityType? FindEntityType(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var lookup = LookupCache.GetValue(_context.Model, BuildLookup);

        if (!lookup.TryGetValue(tableName.Trim(), out var matches))
        {
            return null;
        }

        if (matches.Length > 1)
        {
            var names = string.Join(", ", matches.Select(m => m.ClrType.Name));
            throw new InvalidOperationException(
                $"Table '{tableName}' is mapped by more than one entity type ({names}). " +
                "Resolve it explicitly instead of by table name.");
        }

        return matches[0];
    }

    /// <inheritdoc />
    public IEntityType GetEntityType(string tableName)
        => FindEntityType(tableName)
           ?? throw new InvalidOperationException(
               $"No entity type is mapped to table '{tableName}'. " +
               "Check the name and that the entity is part of this DbContext's model.");

    /// <inheritdoc />
    public ValueTask<object?> FindAsync(string tableName, object keyValue, CancellationToken cancellationToken = default)
        => FindAsync(tableName, [keyValue], cancellationToken);

    /// <inheritdoc />
    public async ValueTask<object?> FindAsync(string tableName, object[] keyValues, CancellationToken cancellationToken = default)
    {
        var (entityType, _, coerced) = Prepare(tableName, keyValues);

        var entity = await _context.FindAsync(entityType.ClrType, coerced, cancellationToken).ConfigureAwait(false);

        // The query filter already excludes soft-deleted rows from the SELECT
        // that Find issues. It does not help when the entity is already tracked,
        // because Find then returns it from the change tracker without querying.
        return entity is IAuditable { IS_DELETED: true } ? null : entity;
    }

    /// <inheritdoc />
    public ValueTask<object?> FindIncludingDeletedAsync(string tableName, object keyValue, CancellationToken cancellationToken = default)
        => FindIncludingDeletedAsync(tableName, [keyValue], cancellationToken);

    /// <inheritdoc />
    public async ValueTask<object?> FindIncludingDeletedAsync(string tableName, object[] keyValues, CancellationToken cancellationToken = default)
    {
        var (entityType, key, coerced) = Prepare(tableName, keyValues);

        // Not Find: it applies global query filters to the SELECT it issues, so
        // a soft-deleted row would never come back. Only an explicit query can
        // opt out, and IgnoreQueryFilters has no Find equivalent.
        var task = (Task<object?>)FindIgnoringFiltersMethod
            .MakeGenericMethod(entityType.ClrType)
            .Invoke(null, [_context, key, coerced, cancellationToken])!;

        return await task.ConfigureAwait(false);
    }

    private (IEntityType EntityType, IKey Key, object?[] Coerced) Prepare(string tableName, object[] keyValues)
    {
        ArgumentNullException.ThrowIfNull(keyValues);

        var entityType = GetEntityType(tableName);

        var key = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"Entity '{entityType.ClrType.Name}' (table '{tableName}') has no primary key, so it cannot be looked up by one.");

        return (entityType, key, Coerce(key, keyValues, tableName));
    }

    private static readonly MethodInfo FindIgnoringFiltersMethod =
        typeof(EntityResolver).GetMethod(nameof(FindIgnoringFilters), BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"{nameof(FindIgnoringFilters)} could not be located.");

    private static readonly MethodInfo EfPropertyMethod =
        typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException("EF.Property<TProperty> could not be located.");

    /// <summary>
    /// Key lookup that opts out of global query filters, so soft-deleted rows
    /// are returned. Generic because <c>Set&lt;T&gt;()</c> has no non-generic
    /// counterpart; invoked through <see cref="FindIgnoringFiltersMethod"/>.
    /// </summary>
    private static async Task<object?> FindIgnoringFilters<TEntity>(
        DbContext context,
        IKey key,
        object?[] keyValues,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? body = null;

        for (var i = 0; i < key.Properties.Count; i++)
        {
            var property = key.Properties[i];

            var left = Expression.Call(
                EfPropertyMethod.MakeGenericMethod(property.ClrType),
                parameter,
                Expression.Constant(property.Name));

            // Field access on a captured holder rather than Expression.Constant:
            // EF inlines constants as SQL literals, which forces Oracle to hard
            // parse a new statement per id. This shape is what a compiler
            // closure looks like, so EF turns it into a bind parameter instead.
            var holder = new KeyValueHolder { Value = keyValues[i] };
            var right = Expression.Convert(
                Expression.Field(Expression.Constant(holder), nameof(KeyValueHolder.Value)),
                property.ClrType);

            var equal = Expression.Equal(left, right);
            body = body is null ? equal : Expression.AndAlso(body, equal);
        }

        var predicate = Expression.Lambda<Func<TEntity, bool>>(body!, parameter);

        return await context.Set<TEntity>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(predicate, cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed class KeyValueHolder
    {
        public object? Value;
    }

    /// <summary>
    /// Converts supplied key values to the key's declared CLR types.
    /// </summary>
    /// <remarks>
    /// Callers routinely hold an <see cref="int"/> while the key is
    /// <see cref="long"/> or <see cref="decimal"/> — Oracle's NUMBER maps to
    /// either depending on precision. <c>FindAsync</c> matches on exact runtime
    /// type, so an unconverted value silently misses.
    /// </remarks>
    private static object?[] Coerce(IKey key, object[] values, string tableName)
    {
        if (key.Properties.Count != values.Length)
        {
            throw new ArgumentException(
                $"Table '{tableName}' has a {key.Properties.Count}-column primary key " +
                $"but {values.Length} value(s) were supplied.",
                nameof(values));
        }

        var result = new object?[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];

            if (value is null)
            {
                result[i] = null;
                continue;
            }

            var target = Nullable.GetUnderlyingType(key.Properties[i].ClrType) ?? key.Properties[i].ClrType;
            result[i] = target.IsInstanceOfType(value) ? value : ConvertTo(value, target, tableName);
        }

        return result;
    }

    private static object ConvertTo(object value, Type target, string tableName)
    {
        try
        {
            return target.IsEnum
                ? Enum.ToObject(target, value)
                : Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new ArgumentException(
                $"Key value '{value}' ({value.GetType().Name}) cannot be converted to " +
                $"{target.Name}, the key type of table '{tableName}'.",
                ex);
        }
    }

    private static IReadOnlyDictionary<string, IEntityType[]> BuildLookup(IModel model)
    {
        var grouped = new Dictionary<string, List<IEntityType>>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, IEntityType type)
        {
            if (!grouped.TryGetValue(name, out var list))
            {
                grouped[name] = list = [];
            }

            if (!list.Contains(type))
            {
                list.Add(type);
            }
        }

        foreach (var entityType in model.GetEntityTypes())
        {
            // Owned types share the owner's table, and TPH subclasses share the
            // root's. Indexing either would report a false ambiguity.
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            var table = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(table))
            {
                continue;
            }

            Add(table, entityType);

            var schema = entityType.GetSchema();
            if (!string.IsNullOrWhiteSpace(schema))
            {
                Add($"{schema}.{table}", entityType);
            }
        }

        return grouped.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }
}
