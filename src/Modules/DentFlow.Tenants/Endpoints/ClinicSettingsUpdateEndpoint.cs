using FastEndpoints;
using MediatR;
using System.Security.Claims;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Tenants.Application;
using DentFlow.Tenants.Application.Commands;

namespace DentFlow.Tenants.Endpoints;

public class UpdateClinicSettingsRequest
{
    public int SlotDurationMinutes { get; init; } = 30;
    /// <summary>JSON string of the weekly per-day schedule.</summary>
    public string? WeeklyScheduleJson { get; init; }
}

/// <summary>
/// Updates clinic working hours. Restricted to ClinicOwner and ClinicAdmin.
/// </summary>
public class ClinicSettingsUpdateEndpoint(ISender sender) : Endpoint<UpdateClinicSettingsRequest, ClinicSettingsResponse>
{
    public override void Configure()
    {
        Put("/tenant/settings");
        Roles(DomainRoles.ClinicOwner, DomainRoles.ClinicAdmin);
        Version(1);
        Summary(s => s.Summary = "Update clinic working-hours settings");
    }

    public override async Task HandleAsync(UpdateClinicSettingsRequest req, CancellationToken ct)
    {
        var tidClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tidClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var result = await sender.Send(
            new UpdateClinicSettingsCommand(tenantId, req.SlotDurationMinutes, req.WeeklyScheduleJson), ct);

        if (result.IsError)
        {
            foreach (var e in result.Errors) AddError(e.Description);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
