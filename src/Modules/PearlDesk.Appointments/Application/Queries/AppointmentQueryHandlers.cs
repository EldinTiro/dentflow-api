using ErrorOr;
using MediatR;
using PearlDesk.Appointments.Application.Interfaces;
using PearlDesk.Appointments.Domain;

namespace PearlDesk.Appointments.Application.Queries;

public class GetAppointmentByIdQueryHandler(IAppointmentRepository repo)
    : IRequestHandler<GetAppointmentByIdQuery, ErrorOr<AppointmentResponse>>
{
    public async Task<ErrorOr<AppointmentResponse>> Handle(
        GetAppointmentByIdQuery query, CancellationToken cancellationToken)
    {
        var appointment = await repo.GetByIdAsync(query.Id, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;
        return AppointmentResponse.FromEntity(appointment);
    }
}

public class ListAppointmentsQueryHandler(IAppointmentRepository repo)
    : IRequestHandler<ListAppointmentsQuery, ErrorOr<PagedResult<AppointmentResponse>>>
{
    public async Task<ErrorOr<PagedResult<AppointmentResponse>>> Handle(
        ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        var (items, total) = await repo.ListAsync(
            query.PatientId, query.ProviderId,
            query.DateFrom, query.DateTo, query.Status,
            query.Page, query.PageSize, cancellationToken);

        var responses = items.Select(AppointmentResponse.FromEntity).ToList();
        return new PagedResult<AppointmentResponse>(responses, total, query.Page, query.PageSize);
    }
}

