using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;
using DentFlow.Identity.Application.Queries;

namespace DentFlow.Identity.Application.Commands;

/// <summary>A user edits their own name (QA-037).</summary>
public record UpdateOwnProfileCommand(Guid UserId, string FirstName, string LastName) : IRequest<ErrorOr<UserProfileResult>>;

public class UpdateOwnProfileCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateOwnProfileCommand, ErrorOr<UserProfileResult>>
{
    public async Task<ErrorOr<UserProfileResult>> Handle(UpdateOwnProfileCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null) return IdentityErrors.UserNotFound;

        user.FirstName = command.FirstName.Trim();
        user.LastName = command.LastName.Trim();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        return new UserProfileResult(user.Id, user.Email!, user.FirstName, user.LastName, user.FullName, user.AvatarUrl, user.LastLoginAt);
    }
}

public class UpdateOwnProfileCommandValidator : AbstractValidator<UpdateOwnProfileCommand>
{
    public UpdateOwnProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("First name is required.").MaximumLength(100);
        RuleFor(x => x.LastName).Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Last name is required.").MaximumLength(100);
    }
}
