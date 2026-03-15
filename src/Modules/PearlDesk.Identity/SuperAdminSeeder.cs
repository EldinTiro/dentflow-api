using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PearlDesk.Domain.Identity;

namespace PearlDesk.Identity;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        // Ensure all roles exist
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed SuperAdmin user from config (or use safe defaults for dev)
        var email = config["SuperAdmin:Email"] ?? "superadmin@pearldesk.local";
        var password = config["SuperAdmin:Password"] ?? "SuperAdmin123!@#";
        var firstName = config["SuperAdmin:FirstName"] ?? "Super";
        var lastName = config["SuperAdmin:LastName"] ?? "Admin";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            logger.LogInformation("SuperAdmin already exists: {Email}", email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create SuperAdmin: {Errors}", errors);
            return;
        }

        await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        logger.LogInformation("SuperAdmin seeded: {Email}", email);
    }
}

