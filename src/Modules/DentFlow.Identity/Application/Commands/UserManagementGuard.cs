using ErrorOr;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;

namespace DentFlow.Identity.Application.Commands;

/// <summary>Who is making a user-management change.</summary>
public record UserManagementActor(Guid UserId, IReadOnlyCollection<string> Roles)
{
    public bool IsSuperAdmin => Roles.Contains(DentFlow.Domain.Identity.Roles.SuperAdmin);
    public bool IsClinicOwner => Roles.Contains(DentFlow.Domain.Identity.Roles.ClinicOwner);
}

/// <summary>
/// Rules shared by role changes and (de)activation (QA-012):
/// nobody changes their own account; only owners (or SuperAdmin) touch owners;
/// a clinic keeps an active owner and the platform keeps an active SuperAdmin.
/// </summary>
internal static class UserManagementGuard
{
    public static async Task<ErrorOr<Success>> CheckAsync(
        UserManager<ApplicationUser> userManager,
        UserManagementActor actor,
        ApplicationUser target,
        IList<string> targetRoles,
        bool removesOwner,
        bool removesSuperAdmin,
        bool grantsOwner)
    {
        if (actor.UserId == target.Id)
            return IdentityErrors.CannotChangeOwnAccount;

        var touchesOwner = targetRoles.Contains(Roles.ClinicOwner) || grantsOwner;
        if (touchesOwner && !actor.IsClinicOwner && !actor.IsSuperAdmin)
            return IdentityErrors.OwnerOnly;

        if (removesOwner && targetRoles.Contains(Roles.ClinicOwner))
        {
            var owners = await userManager.GetUsersInRoleAsync(Roles.ClinicOwner);
            if (!owners.Any(u => u.Id != target.Id && u.IsActive && u.TenantId == target.TenantId))
                return IdentityErrors.LastClinicOwner;
        }

        if (removesSuperAdmin && targetRoles.Contains(Roles.SuperAdmin))
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.SuperAdmin);
            if (!admins.Any(u => u.Id != target.Id && u.IsActive))
                return IdentityErrors.LastSuperAdmin;
        }

        return Result.Success;
    }
}
