using ErrorOr;
using MediatR;

namespace PearlDesk.Appointments.Application.Queries;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<ErrorOr<AppointmentResponse>>;

public record ListAppointmentsQuery(
    Guid? PatientId = null,
    Guid? ProviderId = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 50) : IRequest<ErrorOr<PagedResult<AppointmentResponse>>>;

