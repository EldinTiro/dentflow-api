using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;
using DentFlow.Application.Common;

namespace DentFlow.Identity.Application.Commands;

public record ResetUserPasswordCommand(Guid UserId) : IRequest<ErrorOr<ResetUserPasswordResult>>;

public record ResetUserPasswordResult(string TempPassword);

public class ResetUserPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ResetUserPasswordCommand, ErrorOr<ResetUserPasswordResult>>
{
    public async Task<ErrorOr<ResetUserPasswordResult>> Handle(
        ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null) return IdentityErrors.UserNotFound;

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
