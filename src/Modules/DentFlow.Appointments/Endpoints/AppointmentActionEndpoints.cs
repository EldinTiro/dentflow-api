using FastEndpoints;
using MediatR;
using DentFlow.Appointments.Application;
using DentFlow.Appointments.Application.Commands;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentRescheduleEndpoint(ISender sender) : Endpoint<RescheduleAppointmentRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Put("/appointments/{id}/reschedule");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Reschedule an appointment to a new time slot");
    }

    public override async Task HandleAsync(RescheduleAppointmentRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new RescheduleAppointmentCommand(id, req.NewStartAt, req.NewEndAt), ct);
        if (result.IsError)
        {
            foreach (var error in result.Errors) AddError(error.Description);
            await SendErrorsAsync(cancellation: ct); return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

public record RescheduleAppointmentRequest(DateTime NewStartAt, DateTime NewEndAt);

public class AppointmentCancelEndpoint(ISender sender) : Endpoint<CancelAppointmentRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Post("/appointments/{id}/cancel");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Cancel an appointment");
    }

    public override async Task HandleAsync(CancelAppointmentRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new CancelAppointmentCommand(id, req.Reason, null), ct);
        if (result.IsError)
        {
            foreach (var error in result.Errors) AddError(error.Description);
            await SendErrorsAsync(cancellation: ct); return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

public record CancelAppointmentRequest(string? Reason);

public class AppointmentUpdateStatusEndpoint(ISender sender) : Endpoint<UpdateStatusRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Patch("/appointments/{id}/status");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Update appointment status (confirm, check-in, complete, no-show)");
    }

    public override async Task HandleAsync(UpdateStatusRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new UpdateAppointmentStatusCommand(id, req.Status), ct);
        if (result.IsError)
        {
            foreach (var error in result.Errors) AddError(error.Description);
            await SendErrorsAsync(cancellation: ct); return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdateStatusRequest(string Status);
