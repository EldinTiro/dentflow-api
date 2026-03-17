using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Queries;

public record GetStaffAvailabilityQuery(Guid StaffMemberId)
    : IRequest<ErrorOr<IReadOnlyList<StaffAvailabilityResponse>>>;
