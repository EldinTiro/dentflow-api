using ErrorOr;
using Microsoft.AspNetCore.Identity;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Identity.Application.Commands;

namespace DentFlow.Identity.Application;

public sealed class UserAccountLifecycle(UserManager<ApplicationUser> userManager) : IUserAccountLifecycle
{
    public async Task<ErrorOr<Success>> DeactivateAsync(Guid userId, AccountActor actor, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return Result.Success;

        var roles = await userManager.GetRolesAsync(user);
        var allowed = await UserManagementGuard.CheckAsync(
            userManager, new UserManagementActor(actor.UserId, actor.Roles), user, roles,
            removesOwner: true, removesSuperAdmin: true, grantsOwner: false);
        if (allowed.IsError) return allowed.Errors;

        // Deactivated, not deleted: login history and audit stay (D4). Refresh checks
        // IsActive, so the account's sessions end at their next refresh.
        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> RenameAsync(Guid userId, string firstName, string lastName, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.Success;

        user.FirstName = firstName;
        user.LastName = lastName;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        return Result.Success;
    }
}
