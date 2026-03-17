using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application.Commands;

namespace DentFlow.Staff.Endpoints;

public class StaffDeleteBlockedTimeEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/staff/{id}/blocked-times/{blockedTimeId}");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Remove a blocked time from a staff member");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var blockedTimeId = Route<Guid>("blockedTimeId");
        var result = await sender.Send(new DeleteBlockedTimeCommand(blockedTimeId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendNoContentAsync(ct);
    }
}
