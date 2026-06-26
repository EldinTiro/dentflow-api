using FastEndpoints;
using MediatR;
using System.Security.Claims;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Notifications.Application;
using DentFlow.Notifications.Application.Commands;

namespace DentFlow.Notifications.Endpoints;

public class UpdateNotificationConfigRequest
{
    public bool SmsEnabled { get; init; }
    public int? Reminder1HoursBefore { get; init; }
    public int? Reminder2HoursBefore { get; init; }
}

public class NotificationConfigUpdateEndpoint(ISender sender)
    : Endpoint<UpdateNotificationConfigRequest, NotificationConfigResponse>
{
    public override void Configure()
    {
        Put("/tenant/notification-config");
        Roles(DomainRoles.ClinicOwner, DomainRoles.ClinicAdmin);
        Version(1);
        Summary(s => s.Summary = "Update SMS notification reminder configuration for this clinic");
    }

    public override async Task HandleAsync(UpdateNotificationConfigRequest req, CancellationToken ct)
    {
        var tidClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tidClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var result = await sender.Send(
            new UpdateNotificationConfigCommand(
                tenantId,
                req.SmsEnabled,
                req.Reminder1HoursBefore,
                req.Reminder2HoursBefore), ct);

        if (result.IsError)
        {
            foreach (var e in result.Errors) AddError(e.Description);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
