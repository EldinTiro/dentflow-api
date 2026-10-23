using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Application.Common;

namespace DentFlow.Identity.Application.Commands;

public record InviteTenantUserCommand(
    Guid CallerTenantId,
    string Email,
    string FirstName,
    string LastName,
    string Role) : IRequest<ErrorOr<InviteTenantUserResult>>;

public record InviteTenantUserResult(Guid UserId, string Email, string TempPassword);

public class InviteTenantUserCommandHandler(IUserProvisioningService userProvisioningService)
    : IRequestHandler<InviteTenantUserCommand, ErrorOr<InviteTenantUserResult>>
{
    public async Task<ErrorOr<InviteTenantUserResult>> Handle(
        InviteTenantUserCommand command, CancellationToken ct)
    {
        var tempPassword = TemporaryPassword.Generate();

        var result = await userProvisioningService.CreateUserAsync(
            command.Email, tempPassword,
            command.FirstName, command.LastName,
            command.Role, command.CallerTenantId, ct);

        if (result.IsError) return result.Errors;

        return new InviteTenantUserResult(result.Value, command.Email, tempPassword);
    }
}

public class InviteTenantUserCommandValidator : AbstractValidator<InviteTenantUserCommand>
{
    public InviteTenantUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Roles.TenantRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.TenantRoles)}");
    }
}
