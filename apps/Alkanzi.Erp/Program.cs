using Alkanzi.Erp.Application.Abstractions;
using Alkanzi.Erp.DataAccess;
using Alkanzi.Erp.Domain.Security;
using Alkanzi.Erp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(o =>
{
    // Maps a refused permission to 403 rather than letting it become a 500.
    o.Filters.Add<SecurityExceptionFilter>();

});
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

// The context, Identity's stores, and the EF implementations of the application's ports.
builder.Services.AddErpDataAccess<HttpAuditUserProvider>(connectionString);

// The web app supplies "who is acting" from the request cookie.
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// This host authenticates with the Identity cookie. The data layer registers the stores and
// managers but deliberately picks no scheme, so the API can choose bearer tokens instead.
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "Alkanzi.Erp.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);   // a working day
    o.SlidingExpiration = true;
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/Denied";

    // AJAX callers get a status code rather than the HTML of the login page: the AngularJS
    // front end asks for JSON, and a 200 containing a login form is indistinguishable from
    // real data until it fails to parse.
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (IsApiRequest(ctx.Request))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (IsApiRequest(ctx.Request))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

    static bool IsApiRequest(HttpRequest r) =>
        string.Equals(r.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
        || (r.Headers.Accept.ToString()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Order matters: authentication establishes who the caller is, authorization decides what
// they may reach. Swapped, every [Authorize] would see an anonymous user.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var sp = scope.ServiceProvider;
    var log = sp.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = sp.GetRequiredService<ErpDbContext>();
        await db.Database.MigrateAsync();

        await SecuritySeeder.SeedAsync(
            db,
            sp.GetRequiredService<UserManager<ApplicationUser>>(),
            sp.GetRequiredService<RoleManager<ApplicationRole>>(),
            log,
            adminEmail: builder.Configuration["Seed:AdminEmail"] ?? "obaialkanzi@gmail.com",
            adminPassword: builder.Configuration["Seed:AdminPassword"] ?? "123",
            adminFullName: builder.Configuration["Seed:AdminFullName"] ?? "Obai");

        await DevSeed.SeedAsync(db);
    }
    catch (Exception ex)
    {
        // A database that is not reachable must not stop the app from starting: the failure
        // stays visible in the log instead of becoming a startup crash with no page to read it on.
        log.LogError(ex, "Database initialisation failed. Is PostgreSQL running?");
    }
}

app.Run();
