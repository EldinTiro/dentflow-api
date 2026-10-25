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

public class ProvisionTenantUserCommandHandler(
    IUserProvisioningService userProvisioningService,
    IStaffDirectory staff,
    ITransactionRunner transaction)
    : IRequestHandler<ProvisionTenantUserCommand, ErrorOr<ProvisionTenantUserResult>>
{
    public async Task<ErrorOr<ProvisionTenantUserResult>> Handle(
        ProvisionTenantUserCommand command, CancellationToken ct)
    {
        var tempPassword = TemporaryPassword.Generate();

        // Account and staff profile together, or neither (DT-07).
        return await transaction.ExecuteAsync<ProvisionTenantUserResult>(async innerCt =>
        {
            var result = await userProvisioningService.CreateUserAsync(
                command.Email, tempPassword,
                command.FirstName, command.LastName,
                command.Role, command.TenantId, innerCt);
            if (result.IsError) return result.Errors;

            await staff.CreateLinkedProfileAsync(result.Value, command.TenantId,
                command.FirstName, command.LastName, command.Email, command.Role, innerCt);

            return new ProvisionTenantUserResult(result.Value, command.Email, tempPassword);
        }, ct);
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
