using ErrorOr;
using Microsoft.AspNetCore.Identity;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;

namespace DentFlow.Infrastructure.Services;

public class UserProvisioningService(
    UserManager<ApplicationUser> userManager) : IUserProvisioningService
{
    public async Task<ErrorOr<Guid>> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            // DT-21: don't tell a clinic that this address has an account somewhere on DentFlow.
            return Error.Conflict("User.EmailAlreadyExists", "This email can't be used for a new sign-in. Use a different email.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            TenantId = tenantId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            // Every caller hands the password to someone else; it must be replaced.
            MustChangePassword = true,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return createResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return roleResult.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
        }

        return user.Id;
    }
}
