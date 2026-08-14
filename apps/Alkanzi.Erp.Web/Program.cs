using Alkanzi.Erp.Web.Components;
using Alkanzi.Erp.Web.Infrastructure;
using Alkanzi.ErpServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- ERP services -------------------------------------------------------------
// The approval engine, the procedure service, and the ERP context — pointed at
// the ERP's own connection string ("Erp"), independent of anything else the app
// talks to. CurrentUser / CurrentCompany are this app's tenant + acting-user
// implementations (see Infrastructure/).
var erpConnection = builder.Configuration.GetConnectionString("Erp");
if (string.IsNullOrWhiteSpace(erpConnection))
{
    throw new InvalidOperationException(
        "No 'Erp' connection string configured. Set it before running, e.g.\n" +
        "  dotnet user-secrets set \"ConnectionStrings:Erp\" \"User Id=...;Password=...;Data Source=...\"\n" +
        "or put it under ConnectionStrings:Erp in appsettings.Development.json.\n" +
        "In Docker, set ERP_CONNECTION_STRING in the .env file next to docker-compose.yml\n" +
        "(see .env.example); compose passes it through as ConnectionStrings__Erp.");
}

builder.Services.AddErpApprovalEngine<CurrentUser>();
builder.Services.AddErpProcedureService();
builder.Services.AddErpDbContext(erpConnection);

// Liveness only — deliberately no Oracle probe. The container healthcheck polls
// this every few seconds, and an ERP blip should not make Docker tear down and
// restart a web app that is otherwise perfectly healthy.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Probed by the container HEALTHCHECK over plain HTTP on the loopback. The
// HTTPS redirection above is inert in the container (no HTTPS port is bound),
// so the probe reaches this rather than getting a 307.
app.MapHealthChecks("/healthz").AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
