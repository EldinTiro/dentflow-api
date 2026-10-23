using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Identity;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;
using DentFlow.Identity.Application.Commands;

namespace DentFlow.Identity.Endpoints;

/// <summary>
/// POST /users/{id}/reset-password — the clinic owner resets a team member's password
/// (people-merge plan 2.5). The new password is temporary and must be replaced at sign-in.
/// </summary>
public class ResetTenantUserPasswordEndpoint(ISender sender, UserManager<ApplicationUser> userManager)
    : EndpointWithoutRequest<ResetUserPasswordResult>
{
    public override void Configure()
    {
        Post("/users/{id}/reset-password");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Reset the password of a user in the caller's clinic; returns a temporary password");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(HttpContext.User.FindFirst("tid")?.Value, out var callerTenantId))
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

        var result = await sender.Send(new ResetUserPasswordCommand(targetId, this.Actor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
