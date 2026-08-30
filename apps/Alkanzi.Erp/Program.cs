using Alkanzi.Erp.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// This project holds no database connection on purpose.
//
// It is a presentation client: every read and write goes to Alkanzi.Erp.Api, which is the one
// place that opens the database and the one place that enforces who may do what. Giving the
// web tier its own connection would create a second write path with independently enforced
// rules, hand it credentials it does not need, and quietly make the API optional rather than
// the boundary it is meant to be.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5220";

builder.Services.AddHttpClient(ApiAuthClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    // Short: this runs inside a sign-in, and a user should not sit waiting on an
    // unreachable API before being told so.
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ApiAuthClient>();

// Plain cookie authentication, not Identity: Identity's SignInManager needs a user store,
// and the store lives behind the API now.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "Alkanzi.Erp.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        o.SlidingExpiration = false;   // the bearer token it carries does not slide either
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/Denied";

        // AJAX callers get a status code rather than the HTML of the login page: a 200
        // containing a login form is indistinguishable from real data until it fails to parse.
        o.Events.OnRedirectToLogin = ctx =>
        {
            if (IsApiRequest(ctx.Request)) { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        o.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (IsApiRequest(ctx.Request)) { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };

        static bool IsApiRequest(HttpRequest r) =>
            string.Equals(r.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
            || (r.Headers.Accept.ToString()?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication before authorization: reversed, every [Authorize] would see an anonymous caller.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
