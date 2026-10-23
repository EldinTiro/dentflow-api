using FastEndpoints;
using MediatR;
using System.Security.Claims;
using DentFlow.Tenants.Application;
using DentFlow.Tenants.Application.Queries;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Application.Common.Http;

namespace DentFlow.Tenants.Endpoints;

/// <summary>
/// Returns the feature flags and quotas active for the current tenant's plan.
/// Frontend uses this as the single source of truth for feature gating in the UI.
/// </summary>
public class TenantFeaturesEndpoint(ISender sender) : EndpointWithoutRequest<TenantFeaturesResponse>
{
    public override void Configure()
    {
        Get("/tenant/features");
        Roles(DomainRoles.ClinicOwner, DomainRoles.Dentist, DomainRoles.DentalNurse, DomainRoles.Receptionist);
        Version(1);
        Summary(s => s.Summary = "Get the feature flags and quotas active for the current tenant plan");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tidClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tidClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var result = await sender.Send(new GetTenantFeaturesQuery(tenantId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
