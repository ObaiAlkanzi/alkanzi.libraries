using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Alkanzi.ErpServices;

/// <inheritdoc />
public sealed class ErpProcedureService : IErpProcedureService
{
    private readonly ErpDbContext _context;

    /// <summary>Creates the service over the ERP context's connection.</summary>
    public ErpProcedureService(ErpDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        string routine,
        Func<DbCommand, Task<T>> execute,
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routine);
        ArgumentNullException.ThrowIfNull(execute);

        var connection = _context.Database.GetDbConnection();

        using var command = connection.CreateCommand();
        command.CommandText = routine;
        command.CommandType = commandType;

        // Enlist in the context's transaction if one is open, so a procedure call
        // shares the unit of work — including the rollback the tests rely on, and
        // any SaveChanges the caller batches around it.
        command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

        // Only close what we opened: a connection already open (inside a
        // transaction, or kept open by the caller) is left as we found it.
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await execute(command).ConfigureAwait(false);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> QueryAsync<T>(
        string routine,
        string cursorParameter,
        Func<DbDataReader, T> map,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cursorParameter);
        ArgumentNullException.ThrowIfNull(map);

        return ExecuteAsync<IReadOnlyList<T>>(routine, async command =>
        {
            var oracle = AsOracle(command);
            AddInputs(oracle, parameters);

            var cursor = new OracleParameter(cursorParameter, OracleDbType.RefCursor, ParameterDirection.Output);
            oracle.Parameters.Add(cursor);

            await oracle.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var results = new List<T>();
            if (cursor.Value is OracleRefCursor refCursor)
            {
                // Read while the connection ExecuteAsync opened is still open.
                await using var reader = refCursor.GetDataReader();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(map(reader));
                }
            }

            return results;
        }, commandType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T?> ExecuteScalarProcAsync<T>(
        string routine,
        string outParameter,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outParameter);

        return ExecuteAsync<T?>(routine, async command =>
        {
            var oracle = AsOracle(command);
            AddInputs(oracle, parameters);

            var (oracleType, size) = OutParameterType(typeof(T));
            var outParam = new OracleParameter(outParameter, oracleType, ParameterDirection.Output);
            if (size > 0)
            {
                outParam.Size = size;
            }
            oracle.Parameters.Add(outParam);

            await oracle.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return ConvertOut<T>(outParam.Value);
        }, commandType, cancellationToken);
    }

    // Bind by name so parameters match placeholders by name, not the order they
    // appear — the order in a PL/SQL block is rarely the order they were added.
    private static OracleCommand AsOracle(DbCommand command)
    {
        var oracle = (OracleCommand)command;
        oracle.BindByName = true;
        return oracle;
    }

    private static void AddInputs(OracleCommand command, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new OracleParameter(name, value ?? DBNull.Value));
        }
    }

    private static (OracleDbType Type, int Size) OutParameterType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (t == typeof(string)) return (OracleDbType.Varchar2, 4000);
        if (t == typeof(int)) return (OracleDbType.Int32, 0);
        if (t == typeof(long)) return (OracleDbType.Int64, 0);
        if (t == typeof(decimal)) return (OracleDbType.Decimal, 0);
        if (t == typeof(double)) return (OracleDbType.Double, 0);
        if (t == typeof(DateTime)) return (OracleDbType.Date, 0);

        throw new NotSupportedException(
            $"OUT parameter type '{t.Name}' is not supported by {nameof(ExecuteScalarProcAsync)}. " +
            $"Use {nameof(ExecuteAsync)} and bind the parameter yourself.");
    }

    private static T? ConvertOut<T>(object? value)
    {
        if (value is null or DBNull)
        {
            return default;
        }

        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        switch (value)
        {
            case OracleDecimal d when d.IsNull:
                return default;
            case OracleDecimal d when target == typeof(int):
                return (T)(object)d.ToInt32();
            case OracleDecimal d when target == typeof(long):
                return (T)(object)d.ToInt64();
            case OracleDecimal d when target == typeof(decimal):
                return (T)(object)d.Value;
            case OracleDecimal d when target == typeof(double):
                return (T)(object)d.ToDouble();
            case OracleString s:
                return s.IsNull ? default : (T)(object)s.Value;
            case OracleDate od:
                return od.IsNull ? default : (T)(object)od.Value;
            case OracleTimeStamp ots:
                return ots.IsNull ? default : (T)(object)ots.Value;
        }

        // Provider already handed back a plain CLR value.
        return (T)Convert.ChangeType(value, target);
    }
}
