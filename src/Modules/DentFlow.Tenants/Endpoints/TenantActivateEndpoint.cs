using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Tenants.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Tenants.Endpoints;

public class TenantActivateEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/tenants/{id}/activate");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Re-activate a previously deactivated tenant");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new ActivateTenantCommand(id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
