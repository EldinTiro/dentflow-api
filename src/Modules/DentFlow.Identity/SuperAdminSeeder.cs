using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Domain.Identity;

namespace DentFlow.Identity;

public static class SuperAdminSeeder
{
    /// <summary>
    /// Makes the role table match <see cref="Roles.All"/>: creates missing roles, moves
    /// users out of retired roles (<see cref="Roles.Retired"/>; deactivating where the map
    /// says null) and deletes the retired roles. Idempotent; runs at every startup.
    /// </summary>
    public static async Task SyncRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        foreach (var (retired, replacement) in Roles.Retired)
        {
            var role = await roleManager.FindByNameAsync(retired);
            if (role is null) continue;

            foreach (var user in await userManager.GetUsersInRoleAsync(retired))
            {
                await userManager.RemoveFromRoleAsync(user, retired);
                if (replacement is null)
                {
                    user.IsActive = false;
                    await userManager.UpdateAsync(user);
                    logger.LogWarning("Deactivated {Email}: role {Role} was removed", user.Email, retired);
                }
                else if (!await userManager.IsInRoleAsync(user, replacement))
                {
                    await userManager.AddToRoleAsync(user, replacement);
                    logger.LogInformation("Moved {Email} from {Old} to {New}", user.Email, retired, replacement);
                }
            }

            await roleManager.DeleteAsync(role);
            logger.LogInformation("Removed retired role: {Role}", retired);
        }
    }

    public static async Task SeedAsync(IServiceProvider services)
    {
        await SyncRolesAsync(services);

        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        var email = config["SuperAdmin:Email"];
        var password = config["SuperAdmin:Password"];
        var firstName = config["SuperAdmin:FirstName"] ?? "Super";
        var lastName = config["SuperAdmin:LastName"] ?? "Admin";

        // No defaults: a hardcoded fallback password would be a publicly known admin credential.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogError(
                "SuperAdmin:Email and SuperAdmin:Password are not configured — skipping SuperAdmin seeding. " +
                "Set them via user-secrets (dev) or environment variables (production).");
            return;
        }

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

