using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Domain.Identity;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class ChangeTenantUserRoleEndpoint(ISender sender, UserManager<ApplicationUser> userManager)
    : Endpoint<ChangeTenantUserRoleRequest>
{
    public override void Configure()
    {
        Put("/users/{id}/role");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Change a user's role within the caller's clinic");
    }

    public override async Task HandleAsync(ChangeTenantUserRoleRequest req, CancellationToken ct)
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

        // Prevent escalation to SuperAdmin via this endpoint
        if (!DomainRoles.TenantRoles.Contains(req.Role))
        {
            AddError(r => r.Role, $"Role must be one of: {string.Join(", ", DomainRoles.TenantRoles)}");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var result = await sender.Send(new ChangeUserRoleCommand(targetId, req.Role, this.Actor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}

public record ChangeTenantUserRoleRequest(string Role);
