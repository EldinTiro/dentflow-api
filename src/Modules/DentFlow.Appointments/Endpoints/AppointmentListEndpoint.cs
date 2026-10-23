using FastEndpoints;
using MediatR;
using DentFlow.Appointments.Application;
using DentFlow.Appointments.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Appointments.Endpoints;

public class AppointmentListEndpoint(ISender sender) : EndpointWithoutRequest<PagedResult<AppointmentResponse>>
{
    public override void Configure()
    {
        Get("/appointments");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "List appointments with optional filters");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Query<Guid?>("patientId", isRequired: false);
        var providerId = Query<Guid?>("providerId", isRequired: false);
        var status = Query<string?>("status", isRequired: false);
        var dateFromStr = Query<string?>("dateFrom", isRequired: false);
        var dateToStr = Query<string?>("dateTo", isRequired: false);
        var pageStr = Query<string?>("page", isRequired: false);
        var pageSizeStr = Query<string?>("pageSize", isRequired: false);
        var page = int.TryParse(pageStr, out var p) ? p : 1;
        var pageSize = Math.Min(int.TryParse(pageSizeStr, out var ps) ? ps : 50, 200);

        var dateFrom = DateRangeBound.Parse(dateFromStr, endOfDay: false);
        var dateTo = DateRangeBound.Parse(dateToStr, endOfDay: true);

        var result = await sender.Send(
            new ListAppointmentsQuery(patientId, providerId, dateFrom, dateTo, status, page, pageSize), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
