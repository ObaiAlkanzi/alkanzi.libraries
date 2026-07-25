using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Proves <see cref="ErpProcedureService"/> against real Oracle. Uses anonymous
/// PL/SQL blocks over <c>dual</c> rather than a named ERP procedure, so the
/// assertions are deterministic and no schema object has to exist — but the
/// binding and cursor-reading code is exactly what a call to a real procedure
/// uses.
/// </summary>
[Collection(ErpOracleCollection.Name)]
public class ErpProcedureServiceOracleTests(ErpServicesFixture fixture)
{
    private readonly ErpServicesFixture _fixture = fixture;

    private static ErpProcedureService ProcedureServiceFor(ErpDbContext context)
        => new(context);

    [DockerFact]
    public async Task Reads_a_ref_cursor_returned_by_a_plsql_block()
    {
        await using var context = _fixture.CreateContext();

        // The shape most ERP procedures take: an OUT REF CURSOR the caller reads
        // as a result set. Here the block just opens one over a constant.
        var answer = await ProcedureServiceFor(context).ExecuteAsync(
            "BEGIN OPEN :cur FOR SELECT 42 AS answer FROM dual; END;",
            async command =>
            {
                var oracle = (OracleCommand)command;
                oracle.BindByName = true;

                var cursor = new OracleParameter("cur", OracleDbType.RefCursor, ParameterDirection.Output);
                oracle.Parameters.Add(cursor);

                await oracle.ExecuteNonQueryAsync();

                // Read to completion before the callback returns — the cursor is
                // only valid while the connection the service opened stays open.
                await using var reader = ((OracleRefCursor)cursor.Value).GetDataReader();
                return await reader.ReadAsync() ? reader.GetInt32(0) : -1;
            },
            CommandType.Text);

        Assert.Equal(42, answer);
    }

    [DockerFact]
    public async Task Rounds_an_in_value_out_through_an_out_parameter()
    {
        await using var context = _fixture.CreateContext();

        // IN + OUT scalars: a + b computed in PL/SQL and returned via the OUT
        // bind. BindByName is what lets the binds match the placeholders by name
        // rather than the order they appear in the block.
        var sum = await ProcedureServiceFor(context).ExecuteAsync(
            "BEGIN :result := :a + :b; END;",
            async command =>
            {
                var oracle = (OracleCommand)command;
                oracle.BindByName = true;

                oracle.Parameters.Add(new OracleParameter("result", OracleDbType.Int32, ParameterDirection.Output));
                oracle.Parameters.Add(new OracleParameter("a", OracleDbType.Int32) { Value = 20 });
                oracle.Parameters.Add(new OracleParameter("b", OracleDbType.Int32) { Value = 22 });

                await oracle.ExecuteNonQueryAsync();

                return ((OracleDecimal)oracle.Parameters["result"].Value).ToInt32();
            },
            CommandType.Text);

        Assert.Equal(42, sum);
    }

    [DockerFact]
    public async Task QueryAsync_maps_a_ref_cursor_to_a_list()
    {
        await using var context = _fixture.CreateContext();

        // The typed wrapper hides the cursor plumbing: name the OUT cursor
        // parameter, hand it a row mapper, get a list back.
        var rows = await ProcedureServiceFor(context).QueryAsync(
            "BEGIN OPEN :cur FOR SELECT 1 AS n FROM dual UNION ALL SELECT 2 FROM dual; END;",
            cursorParameter: "cur",
            map: reader => reader.GetInt32(0),
            commandType: CommandType.Text);

        Assert.Equal(new[] { 1, 2 }, rows);
    }

    [DockerFact]
    public async Task ExecuteScalarProcAsync_reads_a_named_out_parameter()
    {
        await using var context = _fixture.CreateContext();

        var sum = await ProcedureServiceFor(context).ExecuteScalarProcAsync<int>(
            "BEGIN :result := :a + :b; END;",
            outParameter: "result",
            parameters: new Dictionary<string, object?> { ["a"] = 20, ["b"] = 22 },
            commandType: CommandType.Text);

        Assert.Equal(42, sum);
    }
}
