using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Staff.Endpoints;

public class StaffDeleteAvailabilityEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/staff/{id}/availability/{availabilityId}");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Remove an availability slot from a staff member");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var availabilityId = Route<Guid>("availabilityId");
        var result = await sender.Send(new DeleteStaffAvailabilityCommand(availabilityId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
