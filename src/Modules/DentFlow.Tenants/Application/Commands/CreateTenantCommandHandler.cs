using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Domain.Tenants;
using DentFlow.Tenants.Application.Interfaces;
using DentFlow.Application.Common;

namespace DentFlow.Tenants.Application.Commands;

public class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUserProvisioningService userProvisioningService)
    : IRequestHandler<CreateTenantCommand, ErrorOr<TenantCreatedResponse>>
{
    public async Task<ErrorOr<TenantCreatedResponse>> Handle(
        CreateTenantCommand command, CancellationToken cancellationToken)
    {
        // Guard: slug must be unique
        var existing = await tenantRepository.GetBySlugAsync(command.Slug, cancellationToken);
        if (existing is not null)
            return TenantErrors.SlugTaken;

        var tenant = Tenant.Create(command.Slug, command.Name);
        tenant.SetPlan(command.Plan, null);
        tenant.SetLogo(command.LogoBase64);

        await tenantRepository.AddAsync(tenant, cancellationToken);

        // Generate a one-time temp password — returned in response body only
        var tempPassword = TemporaryPassword.Generate();

        var userResult = await userProvisioningService.CreateUserAsync(
            command.OwnerEmail,
            tempPassword,
            command.OwnerFirstName,
            command.OwnerLastName,
            Roles.ClinicOwner,
            tenant.Id,
            cancellationToken);

        if (userResult.IsError)
            return userResult.Errors;

        return TenantCreatedResponse.FromEntity(tenant, command.OwnerEmail, tempPassword);
    }
}
