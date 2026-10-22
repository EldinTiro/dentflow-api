using DentFlow.Application.Common.Interfaces;
using ErrorOr;
using MediatR;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Application.Queries;

public class ListAppointmentTypesQueryHandler(IAppointmentTypeRepository repo)
    : IRequestHandler<ListAppointmentTypesQuery, ErrorOr<IReadOnlyList<AppointmentTypeResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<AppointmentTypeResponse>>> Handle(
        ListAppointmentTypesQuery query, CancellationToken cancellationToken)
    {
        var types = await repo.ListAsync(cancellationToken);
        return types.Select(AppointmentTypeResponse.FromEntity).ToList();
    }
}

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

public class ListAppointmentsQueryHandler(IAppointmentRepository repo, IPatientDirectory patients)
    : IRequestHandler<ListAppointmentsQuery, ErrorOr<PagedResult<AppointmentResponse>>>
{
    public async Task<ErrorOr<PagedResult<AppointmentResponse>>> Handle(
        ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        var (items, total) = await repo.ListAsync(
            query.PatientId, query.ProviderId,
            query.DateFrom, query.DateTo, query.Status,
            query.Page, query.PageSize, cancellationToken);

        // Names come with the list; clients used to fetch 500 patients to label it (QA-033).
        var names = await patients.GetNamesAsync(items.Select(a => a.PatientId), cancellationToken);
        var responses = items
            .Select(a => AppointmentResponse.FromEntity(a, names.GetValueOrDefault(a.PatientId)))
            .ToList();
        return new PagedResult<AppointmentResponse>(responses, total, query.Page, query.PageSize);
    }
}

public class GetAppointmentHistoryQueryHandler(IAppointmentRepository repo)
    : IRequestHandler<GetAppointmentHistoryQuery, ErrorOr<IReadOnlyList<AppointmentStatusHistoryResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<AppointmentStatusHistoryResponse>>> Handle(
        GetAppointmentHistoryQuery query, CancellationToken cancellationToken)
    {
        var appointment = await repo.GetByIdAsync(query.AppointmentId, cancellationToken);
        if (appointment is null)
            return AppointmentErrors.NotFound;

        var history = await repo.GetHistoryAsync(query.AppointmentId, cancellationToken);
        return history.Select(AppointmentStatusHistoryResponse.FromEntity).ToList();
    }
}

