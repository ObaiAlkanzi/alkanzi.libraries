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
        "or put it under ConnectionStrings:Erp in appsettings.Development.json.");
}

builder.Services.AddErpApprovalEngine<CurrentUser>();
builder.Services.AddErpProcedureService();
builder.Services.AddErpDbContext(erpConnection);

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
