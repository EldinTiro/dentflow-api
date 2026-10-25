using FastEndpoints;
using MediatR;
using DentFlow.Appointments.Application;
using DentFlow.Appointments.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentTypeListEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<AppointmentTypeResponse>>
{
    public override void Configure()
    {
        Get("/appointment-types");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse");
        Version(1);
        Summary(s => s.Summary = "List all appointment types for the current tenant");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new ListAppointmentTypesQuery(), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
