using FastEndpoints;
using MediatR;
using System.Security.Claims;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Tenants.Application;
using DentFlow.Tenants.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Tenants.Endpoints;

/// <summary>
/// Returns the current authenticated user's tenant (name, slug, logo).
/// Accessible to all tenant roles. SuperAdmin users will receive 404 (no tenant).
/// </summary>
public class TenantCurrentEndpoint(ISender sender) : EndpointWithoutRequest<TenantResponse>
{
    public override void Configure()
    {
        Get("/tenant/current");
        Roles([.. DomainRoles.TenantRoles, DomainRoles.SuperAdmin]);
        Version(1);
        Summary(s => s.Summary = "Get the current user's tenant info including logo");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tidClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tidClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var result = await sender.Send(new GetCurrentTenantQuery(tenantId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
