using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Domain.Identity;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class UpdateTenantUserStatusEndpoint(ISender sender, UserManager<ApplicationUser> userManager)
    : Endpoint<UpdateTenantUserStatusRequest>
{
    public override void Configure()
    {
        Put("/users/{id}/status");
        Roles(DomainRoles.ClinicOwner);
        Version(1);
        Summary(s => s.Summary = "Activate or deactivate a user within the caller's clinic");
    }

    public override async Task HandleAsync(UpdateTenantUserStatusRequest req, CancellationToken ct)
    {
        var tidClaim = HttpContext.User.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tidClaim, out var callerTenantId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var targetId = Route<Guid>("id");
        var target = await userManager.FindByIdAsync(targetId.ToString());
        if (target is null || target.TenantId != callerTenantId)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var result = await sender.Send(new UpdateUserStatusCommand(targetId, req.IsActive, this.Actor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}

public record UpdateTenantUserStatusRequest(bool IsActive);
