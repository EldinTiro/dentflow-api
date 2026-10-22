using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class AdminResetPasswordEndpoint(ISender sender) : EndpointWithoutRequest<ResetPasswordResponse>
{
    public override void Configure()
    {
        Post("/admin/users/{id}/reset-password");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Reset a user's password and return a temporary password");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new ResetUserPasswordCommand(id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(new ResetPasswordResponse(result.Value.TempPassword), ct);
    }
}

public record ResetPasswordResponse(string TempPassword);
