using FastEndpoints;
using MediatR;
using DentFlow.Appointments.Application;
using DentFlow.Appointments.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentBookEndpoint(ISender sender) : Endpoint<BookAppointmentRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Post("/appointments");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Book a new appointment");
    }

    public override async Task HandleAsync(BookAppointmentRequest req, CancellationToken ct)
    {
        var command = new BookAppointmentCommand(
            req.PatientId, req.ProviderId, req.AppointmentTypeId,
            req.StartAt, req.EndAt, req.ChiefComplaint, req.Notes,
            req.OperatoryId, req.IsNewPatient, req.Source ?? "Staff");

        var result = await sender.Send(command, ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendCreatedAtAsync<AppointmentGetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

public record BookAppointmentRequest(
    Guid PatientId, Guid ProviderId, Guid AppointmentTypeId,
    DateTime StartAt, DateTime EndAt, string? ChiefComplaint,
    string? Notes, Guid? OperatoryId, bool IsNewPatient, string? Source);
