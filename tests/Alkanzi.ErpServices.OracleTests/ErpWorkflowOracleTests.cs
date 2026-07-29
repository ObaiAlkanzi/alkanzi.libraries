namespace Alkanzi.ErpServices.OracleTests;

/// <summary>
/// Runs the workflow resolution against the real ERP: the GET_TRANS_WF procedure,
/// the MAP_FUN disambiguation, and the SM_WORKFLOW_FORMS / _LEVELS load. Read-only.
/// </summary>
[Collection(ErpOracleCollection.Name)]
public class ErpWorkflowOracleTests(ErpServicesFixture fixture)
{
    private const int Org = 21;
    private const int Comp = 6;
    private const int Branch = 1;

    // callRegistration is configured with several workflows, so it exercises the
    // MAP_FUN path; id 1 is a live transaction.
    private const string DocType = "callRegistration";
    private const int TransId = 1;

    private readonly ErpServicesFixture _fixture = fixture;

    private IErpApprovalEngine EngineFor(ErpDbContext context)
        => ErpServicesFixture.EngineFor(context, Org, Comp, Branch);

    [DockerFact]
    public async Task GetWorkflows_returns_the_configured_workflows_with_their_map_function()
    {
        await using var context = _fixture.CreateContext();

        var workflows = await EngineFor(context).GetWorkflowsAsync(DocType);

        Assert.NotEmpty(workflows);                               // callRegistration has several
        Assert.All(workflows, w => Assert.NotEqual(0, w.WfId));
        Assert.All(workflows, w => Assert.False(string.IsNullOrWhiteSpace(w.MapFunction)));
    }

    [DockerFact]
    public async Task Resolve_picks_one_workflow_form_and_its_levels()
    {
        await using var context = _fixture.CreateContext();

        var resolved = await EngineFor(context).ResolveWorkflowAsync(DocType, TransId);

        Assert.NotNull(resolved);
        Assert.NotEqual(0, resolved!.WfId);

        // The map function's WF_ID must be one the procedure actually offered.
        var offered = await EngineFor(context).GetWorkflowsAsync(DocType);
        Assert.Contains(resolved.WfId, offered.Select(w => w.WfId));

        // The loaded form is that workflow, and every level belongs to it.
        Assert.Equal(resolved.WfId, resolved.Form.ID);
        Assert.Equal(resolved.Form.LAST_LEVEL, resolved.FinalLevel);
        Assert.All(resolved.Levels, level => Assert.Equal(resolved.WfId, level.FORM_ID));
    }
}
