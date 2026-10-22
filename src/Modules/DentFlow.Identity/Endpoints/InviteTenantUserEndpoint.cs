using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class InviteTenantUserEndpoint(ISender sender)
    : Endpoint<InviteTenantUserRequest, InviteTenantUserResult>
{
    public override void Configure()
    {
        Post("/users/invite");
        Roles(DomainRoles.ClinicOwner, DomainRoles.ClinicAdmin);
        Version(1);
        Summary(s => s.Summary = "Invite a new user to the caller's clinic and receive a temporary password");
    }

    public override async Task HandleAsync(InviteTenantUserRequest req, CancellationToken ct)
    {
        var tidClaim = HttpContext.User.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tidClaim, out var tenantId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var result = await sender.Send(
            new InviteTenantUserCommand(tenantId, req.Email, req.FirstName, req.LastName, req.Role), ct);

        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }

        await SendAsync(result.Value, statusCode: 201, cancellation: ct);
    }
}

public record InviteTenantUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role);
