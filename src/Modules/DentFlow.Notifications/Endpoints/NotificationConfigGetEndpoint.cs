using FastEndpoints;
using MediatR;
using System.Security.Claims;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Notifications.Application;
using DentFlow.Notifications.Application.Queries;

namespace DentFlow.Notifications.Endpoints;

public class NotificationConfigGetEndpoint(ISender sender) : EndpointWithoutRequest<NotificationConfigResponse>
{
    public override void Configure()
    {
        Get("/tenant/notification-config");
        Roles(DomainRoles.ClinicOwner, DomainRoles.ClinicAdmin);
        Version(1);
        Summary(s => s.Summary = "Get SMS notification reminder configuration for this clinic");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tidClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tidClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var result = await sender.Send(new GetNotificationConfigQuery(tenantId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
