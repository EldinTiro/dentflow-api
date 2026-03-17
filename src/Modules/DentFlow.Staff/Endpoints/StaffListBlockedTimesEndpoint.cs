using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Queries;

namespace DentFlow.Staff.Endpoints;

public class StaffListBlockedTimesEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<StaffBlockedTimeResponse>>
{
    public override void Configure()
    {
        Get("/staff/{id}/blocked-times");
        Roles("ClinicOwner", "ClinicAdmin", "Dentist", "Hygienist", "Receptionist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "List blocked times for a staff member");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var from = Query<DateOnly?>("from", isRequired: false);
        var to = Query<DateOnly?>("to", isRequired: false);

        var result = await sender.Send(new ListBlockedTimesQuery(id, from, to), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
