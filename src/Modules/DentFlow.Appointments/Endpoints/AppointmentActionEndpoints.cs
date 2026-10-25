using FastEndpoints;
using MediatR;
using DentFlow.Appointments.Application;
using DentFlow.Appointments.Application.Commands;
using DentFlow.Appointments.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentRescheduleEndpoint(ISender sender) : Endpoint<RescheduleAppointmentRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Put("/appointments/{id}/reschedule");
        Roles("ClinicOwner", "Receptionist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Reschedule an appointment to a new time slot");
    }

    public override async Task HandleAsync(RescheduleAppointmentRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new RescheduleAppointmentCommand(id, req.NewStartAt, req.NewEndAt), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
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
        // Every clinic role: a patient with a visit ahead can't be deactivated (D10, DT-12).
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Cancel an appointment");
    }

    public override async Task HandleAsync(CancelAppointmentRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        Guid? userId = Guid.TryParse(subClaim, out var uid) ? uid : null;
        var result = await sender.Send(new CancelAppointmentCommand(id, req.Reason, userId), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
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
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Advance appointment status following the allowed lifecycle transitions");
    }

    public override async Task HandleAsync(UpdateStatusRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        Guid? userId = Guid.TryParse(subClaim, out var uid) ? uid : null;
        var result = await sender.Send(new UpdateAppointmentStatusCommand(id, req.Status, userId), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdateStatusRequest(string Status);

public class AppointmentOverrideStatusEndpoint(ISender sender) : Endpoint<OverrideStatusRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Post("/appointments/{id}/override-status");
        Roles("ClinicOwner", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Force-set any appointment status (admin override, bypasses lifecycle rules)");
    }

    public override async Task HandleAsync(OverrideStatusRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        Guid? userId = Guid.TryParse(subClaim, out var uid) ? uid : null;
        var result = await sender.Send(new OverrideAppointmentStatusCommand(id, req.NewStatus, req.Reason, userId), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

public record OverrideStatusRequest(string NewStatus, string? Reason);

public class AppointmentUpdateNotesEndpoint(ISender sender) : Endpoint<UpdateNotesRequest, AppointmentResponse>
{
    public override void Configure()
    {
        Patch("/appointments/{id}/notes");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Update the notes field on an appointment");
    }

    public override async Task HandleAsync(UpdateNotesRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        Guid? userId = Guid.TryParse(subClaim, out var uid) ? uid : null;
        var result = await sender.Send(new UpdateAppointmentNotesCommand(id, req.Notes, userId), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdateNotesRequest(string? Notes);

public class AppointmentGetHistoryEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<AppointmentStatusHistoryResponse>>
{
    public override void Configure()
    {
        Get("/appointments/{id}/history");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get the status change history for an appointment");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetAppointmentHistoryQuery(id), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}

