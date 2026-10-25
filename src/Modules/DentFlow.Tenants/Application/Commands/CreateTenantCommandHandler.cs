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
    IUserProvisioningService userProvisioningService,
    IStaffDirectory staff,
    ITransactionRunner transaction)
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

        // Generate a one-time temp password — returned in response body only
        var tempPassword = TemporaryPassword.Generate();

        // Clinic, owner account and the owner's staff profile (DT-07) together, or none.
        return await transaction.ExecuteAsync<TenantCreatedResponse>(async ct =>
        {
            await tenantRepository.AddAsync(tenant, ct);

            var userResult = await userProvisioningService.CreateUserAsync(
                command.OwnerEmail,
                tempPassword,
                command.OwnerFirstName,
                command.OwnerLastName,
                Roles.ClinicOwner,
                tenant.Id,
                ct);
            if (userResult.IsError)
                return userResult.Errors;

            await staff.CreateLinkedProfileAsync(userResult.Value, tenant.Id,
                command.OwnerFirstName, command.OwnerLastName, command.OwnerEmail, Roles.ClinicOwner, ct);

            return TenantCreatedResponse.FromEntity(tenant, command.OwnerEmail, tempPassword);
        }, cancellationToken);
    }
}
