using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application.Commands;

namespace DentFlow.Staff.Endpoints;

public class StaffDeleteAvailabilityEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/staff/{id}/availability/{availabilityId}");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Remove an availability slot from a staff member");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var availabilityId = Route<Guid>("availabilityId");
        var result = await sender.Send(new DeleteStaffAvailabilityCommand(availabilityId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendNoContentAsync(ct);
    }
}
