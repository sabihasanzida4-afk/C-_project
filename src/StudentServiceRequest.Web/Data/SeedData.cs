using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentServiceRequest.Web.Models.Identity;

namespace StudentServiceRequest.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        string[] roles = { "Student", "Staff" };

        // Guard: if Identity tables don't exist yet, fail fast with actionable message instead of 42P01 crash
        try
        {
            // Lightweight probe - will throw 42P01 if AspNetRoles missing
            _ = await roleManager.RoleExistsAsync(roles[0]);
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P01")
        {
            logger.LogError(pgEx, "Seed aborted: relation \"AspNetRoles\" does not exist. Migrations did not create schema. "
                + "Check __EFMigrationsHistory is in sync and use Neon NON-pooled connection (without -pooler) for migrations. "
                + "Current fix: psql -> DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\"='20260905063155_InitialCreate'; then restart app to re-migrate.");
            throw; // rethrow to be caught in Program.cs - avoids silent startup with missing tables
        }

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                logger.LogInformation("Creating role: {Role}", role);
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Create a default staff user for testing
        var staffEmail = "staff@university.edu";
        var staffUser = await userManager.FindByEmailAsync(staffEmail);
        if (staffUser == null)
        {
            staffUser = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                FullName = "University Staff",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(staffUser, "Staff@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
                logger.LogInformation("Created default staff user: {Email}", staffEmail);
            }
            else
            {
                logger.LogError("Failed to create staff user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Fix existing staff user that may be unconfirmed or missing role (from earlier deploys with RequireConfirmedEmail=true)
            bool needsUpdate = false;
            if (!staffUser.EmailConfirmed)
            {
                staffUser.EmailConfirmed = true;
                needsUpdate = true;
                logger.LogInformation("Fixing staff user: setting EmailConfirmed=true for {Email}", staffEmail);
            }
            if (needsUpdate)
            {
                var updResult = await userManager.UpdateAsync(staffUser);
                if (!updResult.Succeeded)
                    logger.LogError("Failed to update staff user {Email}: {Errors}", staffEmail, string.Join(", ", updResult.Errors.Select(e => e.Description)));
            }
            if (!await userManager.IsInRoleAsync(staffUser, "Staff"))
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
                logger.LogInformation("Added Staff role to existing user {Email}", staffEmail);
            }
            // Ensure password still works - if not, log hint (do not auto-reset for security)
            if (!await userManager.CheckPasswordAsync(staffUser, "Staff@123"))
            {
                logger.LogWarning("Existing staff user {Email} password does not match Staff@123 - if you changed it, use that password; otherwise reset via Forgot Password", staffEmail);
            }
        }

        // Auto-fix: confirm all existing users that are still unconfirmed (common cause of "Invalid login attempt" with dummy EmailSender)
        // This makes previous registrations usable without requiring email link from logs.
        var unconfirmedUsers = await userManager.Users.Where(u => !u.EmailConfirmed).ToListAsync();
        foreach (var u in unconfirmedUsers)
        {
            u.EmailConfirmed = true;
            await userManager.UpdateAsync(u);
            logger.LogInformation("Auto-confirmed email for existing user {Email}", u.Email);
        }
    }
}