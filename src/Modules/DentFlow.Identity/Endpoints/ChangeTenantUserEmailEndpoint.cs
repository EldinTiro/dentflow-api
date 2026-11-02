using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Domain.Identity;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class ChangeTenantUserEmailEndpoint(ISender sender, UserManager<ApplicationUser> userManager)
    : Endpoint<ChangeTenantUserEmailRequest>
{
    public override void Configure()
    {
        Put("/users/{id}/email");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Change the sign-in email of a user in the caller's clinic (the linked profile follows)");
    }

    public override async Task HandleAsync(ChangeTenantUserEmailRequest req, CancellationToken ct)
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

        var result = await sender.Send(new ChangeUserEmailCommand(targetId, req.Email ?? string.Empty, this.Actor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}

public record ChangeTenantUserEmailRequest(string? Email);
