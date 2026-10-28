using FastEndpoints;
using MediatR;
using DentFlow.Appointments.Application;
using DentFlow.Appointments.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentTypeCreateRequest
{
    public string Name { get; set; } = default!;
    public int DefaultDurationMinutes { get; set; }
    public string? Description { get; set; }
    public string? ColorHex { get; set; }
    public bool IsBookableOnline { get; set; }
    public decimal? DefaultFee { get; set; }
}

public class AppointmentTypeUpdateRequest
{
    public string Name { get; set; } = default!;
    public int DefaultDurationMinutes { get; set; }
    public string? Description { get; set; }
    public string? ColorHex { get; set; }
    public bool IsBookableOnline { get; set; }
    public decimal? DefaultFee { get; set; }
}

public class AppointmentTypeCreateEndpoint(ISender sender)
    : Endpoint<AppointmentTypeCreateRequest, AppointmentTypeResponse>
{
    public override void Configure()
    {
        Post("/appointment-types");
        Roles("ClinicOwner");
        Version(1);
        Summary(s => s.Summary = "Create a new appointment type");
    }

    public override async Task HandleAsync(AppointmentTypeCreateRequest req, CancellationToken ct)
    {
        var command = new CreateAppointmentTypeCommand(
            req.Name,
            req.DefaultDurationMinutes,
            req.Description,
            req.ColorHex,
            req.IsBookableOnline,
            req.DefaultFee);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendCreatedAtAsync<AppointmentTypeListEndpoint>(null, result.Value, cancellation: ct);
    }
}

public class AppointmentTypeUpdateEndpoint(ISender sender)
    : Endpoint<AppointmentTypeUpdateRequest, AppointmentTypeResponse>
{
    public override void Configure()
    {
        Put("/appointment-types/{id}");
        Roles("ClinicOwner");
        Version(1);
        Summary(s => s.Summary = "Update an appointment type");
    }

    public override async Task HandleAsync(AppointmentTypeUpdateRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var command = new UpdateAppointmentTypeCommand(
            id,
            req.Name,
            req.DefaultDurationMinutes,
            req.Description,
            req.ColorHex,
            req.IsBookableOnline,
            req.DefaultFee);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public class AppointmentTypeDeleteEndpoint(ISender sender)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/appointment-types/{id}");
        Roles("ClinicOwner");
        Version(1);
        Summary(s => s.Summary = "Soft-delete an appointment type");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new DeleteAppointmentTypeCommand(id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
