using FastEndpoints;
using MediatR;
using System.Security.Claims;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Tenants.Application;
using DentFlow.Tenants.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Tenants.Endpoints;

/// <summary>
/// Returns the clinic's working hours and slot duration.
/// Readable by all tenant staff; SuperAdmin gets 404.
/// </summary>
public class ClinicSettingsGetEndpoint(ISender sender) : EndpointWithoutRequest<ClinicSettingsResponse>
{
    public override void Configure()
    {
        Get("/tenant/settings");
        Roles(DomainRoles.ClinicOwner, DomainRoles.Dentist, DomainRoles.DentalNurse, DomainRoles.Receptionist);
        Version(1);
        Summary(s => s.Summary = "Get clinic working-hours settings");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tidClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tidClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var result = await sender.Send(new GetClinicSettingsQuery(tenantId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
