using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;

namespace DentFlow.Identity.Application.Commands;

public record UpdateUserStatusCommand(Guid UserId, bool IsActive, UserManagementActor Actor) : IRequest<ErrorOr<Updated>>;

public class UpdateUserStatusCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateUserStatusCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateUserStatusCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null) return IdentityErrors.UserNotFound;

        var roles = await userManager.GetRolesAsync(user);
        var deactivating = !command.IsActive;
        var allowed = await UserManagementGuard.CheckAsync(
            userManager, command.Actor, user, roles,
            removesOwner: deactivating, removesSuperAdmin: deactivating, grantsOwner: false);
        if (allowed.IsError) return allowed.Errors;

        user.IsActive = command.IsActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        return Result.Updated;
    }
}
