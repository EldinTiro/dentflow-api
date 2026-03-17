using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Queries;

public class ListBlockedTimesQueryHandler(IStaffRepository staffRepository)
    : IRequestHandler<ListBlockedTimesQuery, ErrorOr<IReadOnlyList<StaffBlockedTimeResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<StaffBlockedTimeResponse>>> Handle(
        ListBlockedTimesQuery query,
        CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(query.StaffMemberId, cancellationToken);
        if (staff is null)
            return StaffErrors.NotFound;

        var blockedTimes = await staffRepository.GetBlockedTimesAsync(
            query.StaffMemberId, query.From, query.To, cancellationToken);

        return blockedTimes.Select(StaffBlockedTimeResponse.FromEntity).ToList();
    }
}
