using FastEndpoints;
using MediatR;
using PearlDesk.Appointments.Application;
using PearlDesk.Appointments.Application.Queries;

namespace PearlDesk.Appointments.Endpoints;

public class AppointmentGetByIdEndpoint(ISender sender) : EndpointWithoutRequest<AppointmentResponse>
{
    public override void Configure()
    {
        Get("/appointments/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get an appointment by ID");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetAppointmentByIdQuery(id), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
