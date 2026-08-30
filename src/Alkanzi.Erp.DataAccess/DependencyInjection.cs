using Alkanzi.Auditable.EntityFrameworkCore;
using Alkanzi.Erp.Application.Abstractions;
using Alkanzi.Erp.DataAccess.Search;
using Alkanzi.Erp.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alkanzi.Erp.DataAccess;

/// <summary>
/// Composition for the data-access layer: the context, Identity's stores, and the EF
/// implementations of the application's ports. The web project calls this instead of knowing
/// which provider or store types are in play.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddErpDataAccess<TAuditUserProvider>(
        this IServiceCollection services,
        string connectionString)
        where TAuditUserProvider : class, IAuditUserProvider
    {
        // Stamps IAuditable entities and rewrites deletes into soft deletes. Emits no SQL of
        // its own, so it is provider-agnostic.
        services.AddAuditable<TAuditUserProvider>(o =>
        {
            o.SoftDelete = true;
            o.SystemUserId = 0;   // seeding, jobs, health checks
        });

        services.AddDbContext<ErpDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            // PostgreSQL folds unquoted identifiers to lower case, so PascalCase names would
            // need quoting in every hand-written query. snake_case keeps the schema idiomatic.
            .UseSnakeCaseNamingConvention()
            // Two interceptors, and the order is intentional: the audit one rewrites deletes
            // into soft deletes first, so the index interceptor sees IS_DELETED and drops the
            // document rather than missing the removal.
            .AddInterceptors(
                sp.GetRequiredService<AuditableSaveChangesInterceptor>(),
                sp.GetRequiredService<SearchIndexInterceptor>()));

        // AddIdentityCore, not AddIdentity. AddIdentity registers cookie handlers AND sets
        // AuthenticationOptions.DefaultAuthenticateScheme to the Identity cookie — which is a
        // decision about how requests are authenticated, and that belongs to the host, not to
        // the data layer. It silently broke the API: [Authorize] resolved to the cookie
        // handler, found no cookie, and returned 401 without ever inspecting the bearer token,
        // even though AddAuthentication(JwtBearer) had been called afterwards.
        //
        // Core registers the stores and managers and touches no schemes. The MVC app adds
        // AddIdentityCookies(); the API adds AddJwtBearer(). Each host picks its own.
        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.SignIn.RequireConfirmedAccount = false;

            // WARNING: this policy accepts a three-digit password such as "123", which is
            // what the seeded account uses. That is fine on a developer's machine and
            // indefensible anywhere else — a password this short falls to an offline guess
            // instantly, and Identity's lockout only protects the online path.
            //
            // Before this application is reachable by anyone else, restore something like
            // RequiredLength = 12 with the character classes re-enabled, and change the
            // seeded account's password.
            o.Password.RequiredLength = 3;
            o.Password.RequireDigit = false;
            o.Password.RequireLowercase = false;
            o.Password.RequireUppercase = false;
            o.Password.RequireNonAlphanumeric = false;

            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            o.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ErpDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        // Singleton: it holds no per-request state, keying its in-flight work by context.
        services.AddSingleton<SearchIndexInterceptor>();

        services.AddScoped<SearchService>();

        return services;
    }
}
