using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;
using DentFlow.Application.Common;

namespace DentFlow.Identity.Application.Commands;

/// <param name="Actor">
/// When set, the user-management guards apply: nobody resets their own password here
/// (Profile → Change password does that), and only owners touch an owner's account.
/// </param>
public record ResetUserPasswordCommand(Guid UserId, UserManagementActor? Actor = null) : IRequest<ErrorOr<ResetUserPasswordResult>>;

public record ResetUserPasswordResult(string TempPassword);

public class ResetUserPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ResetUserPasswordCommand, ErrorOr<ResetUserPasswordResult>>
{
    public async Task<ErrorOr<ResetUserPasswordResult>> Handle(
        ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null) return IdentityErrors.UserNotFound;

        if (command.Actor is { } actor)
        {
            var roles = await userManager.GetRolesAsync(user);
            var allowed = await UserManagementGuard.CheckAsync(
                userManager, actor, user, roles, removesOwner: false, removesSuperAdmin: false, grantsOwner: false);
            if (allowed.IsError) return allowed.Errors;
        }

        var tempPassword = TemporaryPassword.Generate();
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, tempPassword);

        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);

        return new ResetUserPasswordResult(tempPassword);
    }
}
