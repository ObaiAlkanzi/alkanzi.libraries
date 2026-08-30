using Alkanzi.Erp.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alkanzi.Erp.DataAccess;

/// <summary>
/// Brings the system up to a usable baseline: a company with branches, the built-in Identity
/// roles, and the super-administrator account.
/// <para>
/// Idempotent — it reconciles rather than inserting blindly, so it is safe on every startup.
/// </para>
/// </summary>
public static class SecuritySeeder
{
    public const string SuperAdminRole = "Super Admin";

    public static async Task SeedAsync(
        ErpDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ILogger logger,
        string adminEmail,
        string adminPassword,
        string adminFullName,
        CancellationToken ct = default)
    {
        var company = await SeedCompanyAsync(db, ct).ConfigureAwait(false);
        await SeedRolesAsync(roles).ConfigureAwait(false);
        await SeedSuperAdminAsync(db, users, logger, company.Id, adminEmail, adminPassword, adminFullName, ct).ConfigureAwait(false);
    }

    private static async Task<Company> SeedCompanyAsync(ErpDbContext db, CancellationToken ct)
    {
        var company = await db.Companies.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (company is null)
        {
            company = new Company { Code = "ALK", Name = "Alkanzi Holdings", Currency = "AED" };
            db.Companies.Add(company);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (!await db.Branches.AnyAsync(b => b.CompanyId == company.Id, ct).ConfigureAwait(false))
        {
            db.Branches.AddRange(
                new Branch { CompanyId = company.Id, Code = "HO", Name = "Head Office" },
                new Branch { CompanyId = company.Id, Code = "DXB", Name = "Dubai" },
                new Branch { CompanyId = company.Id, Code = "AUH", Name = "Abu Dhabi" });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return company;
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roles)
    {
        foreach (var (name, description, system) in new[]
        {
            (SuperAdminRole, "Unrestricted access to every module.", true),
            ("Buyer",        "Raises and edits purchase orders.",    false),
            ("Approver",     "Approves documents.",                  false),
            ("Viewer",       "Read-only access.",                    false),
        })
        {
            if (!await roles.RoleExistsAsync(name).ConfigureAwait(false))
            {
                await roles.CreateAsync(new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    IsSystemRole = system,
                }).ConfigureAwait(false);
            }
        }
    }

    private static async Task SeedSuperAdminAsync(
        ErpDbContext db, UserManager<ApplicationUser> users, ILogger logger,
        int companyId, string email, string password, string fullName, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
        {
            var headOffice = await db.Branches.FirstOrDefaultAsync(b => b.CompanyId == companyId, ct).ConfigureAwait(false);

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                CompanyId = companyId,
                BranchId = headOffice?.Id,
                IsActive = true,
            };

            var result = await users.CreateAsync(user, password).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                // Surfaced rather than swallowed: a failure here means nobody can sign in, and
                // the reason — almost always the password policy — has to be visible.
                logger.LogError("Security seed: could not create {Email} — {Errors}",
                    email, string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Security seed: created {Email} ({Name}).", email, fullName);
        }

        if (!await users.IsInRoleAsync(user, SuperAdminRole).ConfigureAwait(false))
        {
            await users.AddToRoleAsync(user, SuperAdminRole).ConfigureAwait(false);
            logger.LogInformation("Security seed: added {Email} to {Role}.", email, SuperAdminRole);
        }
    }
}
