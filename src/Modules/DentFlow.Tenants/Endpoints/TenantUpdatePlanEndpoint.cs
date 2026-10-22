using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Tenants.Application;
using DentFlow.Tenants.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Tenants.Endpoints;

public class TenantUpdatePlanEndpoint(ISender sender) : Endpoint<UpdateTenantPlanRequest, TenantResponse>
{
    public override void Configure()
    {
        Put("/tenants/{id}/plan");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Update a tenant's subscription plan");
    }

    public override async Task HandleAsync(UpdateTenantPlanRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new UpdateTenantPlanCommand(id, req.Plan, req.ExpiresAt), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdateTenantPlanRequest(string Plan, DateTime? ExpiresAt);
