using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;

namespace DentFlow.Identity.Application.Commands;

// ── Command: change own password ───────────────────────────────────────────

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<ErrorOr<Updated>>;

public class ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ChangePasswordCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            return IdentityErrors.UserNotFound;

        // DT-04: keeping the temporary password means whoever handed it out still knows it.
        if (command.NewPassword == command.CurrentPassword)
            return IdentityErrors.PasswordUnchanged;

        var result = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!result.Succeeded)
            return result.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await userManager.UpdateAsync(user);
        }

        return Result.Updated;
    }
}
