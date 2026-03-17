using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Queries;

public record ListBlockedTimesQuery(
    Guid StaffMemberId,
    DateOnly? From = null,
    DateOnly? To = null)
    : IRequest<ErrorOr<IReadOnlyList<StaffBlockedTimeResponse>>>;
