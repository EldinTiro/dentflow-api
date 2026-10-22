using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class AdminUpdateUserStatusEndpoint(ISender sender) : Endpoint<UpdateUserStatusRequest>
{
    public override void Configure()
    {
        Put("/admin/users/{id}/status");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Activate or deactivate a user account");
    }

    public override async Task HandleAsync(UpdateUserStatusRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new UpdateUserStatusCommand(id, req.IsActive, this.Actor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}

public record UpdateUserStatusRequest(bool IsActive);
