using System.Data;
using System.Data.Common;

namespace Alkanzi.ErpServices;

/// <summary>
/// Runs Oracle stored procedures and functions over the ERP context's
/// connection.
/// </summary>
/// <remarks>
/// The ERP calls many procedures, of many shapes — IN parameters, OUT scalars,
/// and REF CURSOR result sets. Rather than a method per shape, this exposes one
/// primitive that manages the connection, command type and ambient transaction,
/// and hands the caller the live <see cref="DbCommand"/> to bind and read
/// however the routine requires. Provider-agnostic on the surface; cast the
/// command to <c>OracleCommand</c> inside the callback for Oracle specifics
/// (<c>BindByName</c>, <c>OracleDbType.RefCursor</c>, and the like).
/// </remarks>
public interface IErpProcedureService
{
    /// <summary>
    /// Opens the connection if needed, creates a command for <paramref name="routine"/>,
    /// enlists it in the context's current transaction if there is one, and runs
    /// <paramref name="execute"/> against it.
    /// </summary>
    /// <typeparam name="T">What the callback pulls back — a scalar, a mapped list, anything.</typeparam>
    /// <param name="routine">
    /// Procedure or function name (optionally <c>PACKAGE.NAME</c>), or raw PL/SQL
    /// when <paramref name="commandType"/> is <see cref="CommandType.Text"/>.
    /// </param>
    /// <param name="execute">
    /// Binds parameters, executes, and reads results off the command. Any results
    /// tied to the open connection (a REF CURSOR reader, for one) must be read to
    /// completion here, before the callback returns.
    /// </param>
    /// <param name="commandType">
    /// <see cref="CommandType.StoredProcedure"/> by default; pass
    /// <see cref="CommandType.Text"/> to run a PL/SQL block or ad-hoc statement.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> ExecuteAsync<T>(
        string routine,
        Func<DbCommand, Task<T>> execute,
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a routine whose OUT <paramref name="cursorParameter"/> is a REF
    /// CURSOR, and maps each row of it to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Row type produced by <paramref name="map"/>.</typeparam>
    /// <param name="routine">Procedure name, or PL/SQL when <paramref name="commandType"/> is Text.</param>
    /// <param name="cursorParameter">Name of the OUT REF CURSOR parameter.</param>
    /// <param name="map">Turns one reader row into a <typeparamref name="T"/>.</param>
    /// <param name="parameters">IN parameters by name, bound by name.</param>
    /// <param name="commandType">Stored procedure by default; Text for a PL/SQL block.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string routine,
        string cursorParameter,
        Func<DbDataReader, T> map,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a routine and returns the value of a single OUT parameter, converted
    /// to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Result type. Supported: <see cref="string"/>, <see cref="int"/>,
    /// <see cref="long"/>, <see cref="decimal"/>, <see cref="double"/>,
    /// <see cref="DateTime"/> (and their nullable forms). For anything else, use
    /// <see cref="ExecuteAsync{T}"/> and read the parameter yourself.
    /// </typeparam>
    /// <param name="routine">Procedure name, or PL/SQL when <paramref name="commandType"/> is Text.</param>
    /// <param name="outParameter">Name of the OUT parameter to read.</param>
    /// <param name="parameters">IN parameters by name, bound by name.</param>
    /// <param name="commandType">Stored procedure by default; Text for a PL/SQL block.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> ExecuteScalarProcAsync<T>(
        string routine,
        string outParameter,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default);
}
