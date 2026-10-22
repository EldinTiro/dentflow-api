using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Identity.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class AdminProvisionTenantUserEndpoint(ISender sender)
    : Endpoint<ProvisionTenantUserRequest, ProvisionTenantUserResult>
{
    public override void Configure()
    {
        Post("/admin/tenants/{id}/users");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Add a user to a tenant and return a temporary password");
    }

    public override async Task HandleAsync(ProvisionTenantUserRequest req, CancellationToken ct)
    {
        var tenantId = Route<Guid>("id");
        var result = await sender.Send(
            new ProvisionTenantUserCommand(tenantId, req.Email, req.FirstName, req.LastName, req.Role), ct);

        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }

        await SendAsync(result.Value, statusCode: 201, cancellation: ct);
    }
}

public record ProvisionTenantUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role);
