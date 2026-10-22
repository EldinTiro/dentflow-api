using ErrorOr;
using MediatR;

namespace DentFlow.Appointments.Application.Queries;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<ErrorOr<AppointmentResponse>>;

public record ListAppointmentsQuery(
    Guid? PatientId = null,
    Guid? ProviderId = null,
    DateTime? DateFrom = null, // UTC, inclusive
    DateTime? DateTo = null,   // UTC, inclusive
    string? Status = null,
    int Page = 1,
    int PageSize = 50) : IRequest<ErrorOr<PagedResult<AppointmentResponse>>>;

public record ListAppointmentTypesQuery : IRequest<ErrorOr<IReadOnlyList<AppointmentTypeResponse>>>;

public record GetAppointmentHistoryQuery(Guid AppointmentId)
    : IRequest<ErrorOr<IReadOnlyList<AppointmentStatusHistoryResponse>>>;

