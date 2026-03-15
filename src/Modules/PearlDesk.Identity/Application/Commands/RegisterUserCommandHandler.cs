using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Identity;
using PearlDesk.Domain.Identity;

namespace PearlDesk.Identity.Application.Commands;

public class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<RegisterUserCommand, ErrorOr<RegisterUserResult>>
{
    public async Task<ErrorOr<RegisterUserResult>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // Guard: duplicate email
        var existing = await userManager.FindByEmailAsync(command.Email);
        if (existing is not null)
            return IdentityErrors.EmailAlreadyExists;

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            TenantId = command.TenantId,
            IsActive = true,
            EmailConfirmed = true, // TODO: replace with email-verification flow via SES
            CreatedAt = DateTime.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();
            return errors;
        }

        var roleResult = await userManager.AddToRoleAsync(user, command.Role);
        if (!roleResult.Succeeded)
        {
            // Roll back: delete user if role assignment fails
            await userManager.DeleteAsync(user);
            var errors = roleResult.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();
            return errors;
        }

        return new RegisterUserResult(
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Role: command.Role);
    }
}

