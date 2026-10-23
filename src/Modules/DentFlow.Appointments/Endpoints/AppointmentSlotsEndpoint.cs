using FastEndpoints;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentSlotsEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<TimeSlot>>
{
    public override void Configure()
    {
        Get("/staff/{providerId}/slots");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get available appointment slots for a provider on a given date.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var providerId = Route<Guid>("providerId");
        var dateStr = Query<string?>("date", isRequired: false);
        var duration = Query<int?>("duration", isRequired: false) ?? 30;

        if (!DateOnly.TryParse(dateStr, out var date))
            date = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await sender.Send(new GetAvailableSlotsQuery(providerId, date, duration), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
