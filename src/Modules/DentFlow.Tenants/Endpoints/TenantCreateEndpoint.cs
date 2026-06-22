using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Tenants.Application;
using DentFlow.Tenants.Application.Commands;

namespace DentFlow.Tenants.Endpoints;

public class TenantCreateEndpoint(ISender sender) : Endpoint<CreateTenantRequest, TenantCreatedResponse>
{
    public override void Configure()
    {
        Post("/tenants");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Create a new tenant and provision its ClinicOwner account");
    }

    public override async Task HandleAsync(CreateTenantRequest req, CancellationToken ct)
    {
        var command = new CreateTenantCommand(
            req.Slug, req.Name, req.Plan,
            req.OwnerEmail, req.OwnerFirstName, req.OwnerLastName,
            req.LogoBase64);

        var result = await sender.Send(command, ct);
        if (result.IsError)
        {
            foreach (var e in result.Errors)
                AddError(e.Code, e.Description);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendCreatedAtAsync<TenantGetByIdEndpoint>(
            new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

public record CreateTenantRequest(
    string Slug,
    string Name,
    string Plan,
    string OwnerEmail,
    string OwnerFirstName,
    string OwnerLastName,
    string? LogoBase64 = null);
