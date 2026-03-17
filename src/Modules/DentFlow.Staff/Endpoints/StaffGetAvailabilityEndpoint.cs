using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Queries;

namespace DentFlow.Staff.Endpoints;

public class StaffGetAvailabilityEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<StaffAvailabilityResponse>>
{
    public override void Configure()
    {
        Get("/staff/{id}/availability");
        Roles("ClinicOwner", "ClinicAdmin", "Dentist", "Hygienist", "Receptionist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get all availability slots for a staff member");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetStaffAvailabilityQuery(id), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
