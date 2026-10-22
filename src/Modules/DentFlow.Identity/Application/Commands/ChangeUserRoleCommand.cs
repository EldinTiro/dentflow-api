using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Identity.Application.Commands;

public record ChangeUserRoleCommand(Guid UserId, string NewRole, UserManagementActor Actor) : IRequest<ErrorOr<Updated>>;

public class ChangeUserRoleCommandHandler(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    : IRequestHandler<ChangeUserRoleCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null) return IdentityErrors.UserNotFound;

        if (!Roles.All.Contains(command.NewRole))
            return Error.Validation("Role.Invalid", $"'{command.NewRole}' is not a valid role.");

        if (command.NewRole == Roles.SuperAdmin && user.TenantId is not null)
            return IdentityErrors.SuperAdminForClinicUser;

        var currentRoles = await userManager.GetRolesAsync(user);
        var allowed = await UserManagementGuard.CheckAsync(
            userManager, command.Actor, user, currentRoles,
            removesOwner: command.NewRole != Roles.ClinicOwner,
            removesSuperAdmin: command.NewRole != Roles.SuperAdmin,
            grantsOwner: command.NewRole == Roles.ClinicOwner);
        if (allowed.IsError) return allowed.Errors;

        // Remove + add in one transaction: a failed add used to leave the user with no role.
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        if (currentRoles.Any())
        {
            var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removed.Succeeded)
                return removed.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
        }

        var result = await userManager.AddToRoleAsync(user, command.NewRole);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        await tx.CommitAsync(cancellationToken);
        return Result.Updated;
    }
}
