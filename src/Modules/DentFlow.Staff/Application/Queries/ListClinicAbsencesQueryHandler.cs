using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;

namespace DentFlow.Staff.Application.Queries;

public class ListClinicAbsencesQueryHandler(IStaffRepository staffRepository)
    : IRequestHandler<ListClinicAbsencesQuery, ErrorOr<IReadOnlyList<StaffBlockedTimeResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<StaffBlockedTimeResponse>>> Handle(
        ListClinicAbsencesQuery query,
        CancellationToken cancellationToken)
    {
        var absences = await staffRepository.GetClinicBlockedTimesAsync(query.From, query.To, cancellationToken);
        return absences.Select(StaffBlockedTimeResponse.FromEntity).ToList();
    }
}
