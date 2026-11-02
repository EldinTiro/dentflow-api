using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Identity.Application.Commands;

/// <summary>
/// Changes the email someone signs in with, and the linked profile's email with it
/// (user report 2026-11-02: a wrong sign-in email could not be corrected anywhere).
/// </summary>
public record ChangeUserEmailCommand(Guid UserId, string NewEmail, UserManagementActor Actor) : IRequest<ErrorOr<Updated>>;

public class ChangeUserEmailCommandValidator : AbstractValidator<ChangeUserEmailCommand>
{
    public ChangeUserEmailCommandValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public class ChangeUserEmailCommandHandler(
    UserManager<ApplicationUser> userManager, IStaffDirectory staffDirectory, ITransactionRunner transaction)
    : IRequestHandler<ChangeUserEmailCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(ChangeUserEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null) return IdentityErrors.UserNotFound;

        // Same rules as a role change: not your own account here, only owners touch owners
        var allowed = await UserManagementGuard.CheckAsync(
            userManager, command.Actor, user, await userManager.GetRolesAsync(user),
            removesOwner: false, removesSuperAdmin: false, grantsOwner: false);
        if (allowed.IsError) return allowed.Errors;

        var email = command.NewEmail.Trim();
        if (user.Email == email) return Result.Updated; // a case-only fix still goes through

        var owner = await userManager.FindByEmailAsync(email);
        if (owner is not null && owner.Id != user.Id) return IdentityErrors.EmailAlreadyExists;

        return await transaction.ExecuteAsync<Updated>(async ct =>
        {
            // The user name is the email: sign-in looks the account up by either
            var setEmail = await userManager.SetEmailAsync(user, email);
            if (!setEmail.Succeeded) return Failed(setEmail);
            var setName = await userManager.SetUserNameAsync(user, email);
            if (!setName.Succeeded) return Failed(setName);

            if (!await staffDirectory.ChangeLinkedProfileEmailAsync(user.Id, email, ct))
                return IdentityErrors.EmailAlreadyExists;

            return Result.Updated;
        }, cancellationToken);
    }

    private static List<Error> Failed(IdentityResult result) =>
        result.Errors.Select(e => e.Code is "DuplicateEmail" or "DuplicateUserName"
            ? IdentityErrors.EmailAlreadyExists
            : Error.Validation(e.Code, e.Description)).ToList();
}
