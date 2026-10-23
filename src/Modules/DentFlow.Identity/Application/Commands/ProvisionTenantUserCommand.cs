using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Application.Common;

namespace DentFlow.Identity.Application.Commands;

public record ProvisionTenantUserCommand(
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string Role) : IRequest<ErrorOr<ProvisionTenantUserResult>>;

public record ProvisionTenantUserResult(Guid UserId, string Email, string TempPassword);

public class ProvisionTenantUserCommandHandler(IUserProvisioningService userProvisioningService)
    : IRequestHandler<ProvisionTenantUserCommand, ErrorOr<ProvisionTenantUserResult>>
{
    public async Task<ErrorOr<ProvisionTenantUserResult>> Handle(
        ProvisionTenantUserCommand command, CancellationToken ct)
    {
        var tempPassword = TemporaryPassword.Generate();

        var result = await userProvisioningService.CreateUserAsync(
            command.Email, tempPassword,
            command.FirstName, command.LastName,
            command.Role, command.TenantId, ct);

        if (result.IsError) return result.Errors;

        return new ProvisionTenantUserResult(result.Value, command.Email, tempPassword);
    }
}

public class ProvisionTenantUserCommandValidator : AbstractValidator<ProvisionTenantUserCommand>
{
    public ProvisionTenantUserCommandValidator()
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
