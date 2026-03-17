using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Queries;

public class GetStaffAvailabilityQueryHandler(IStaffRepository staffRepository)
    : IRequestHandler<GetStaffAvailabilityQuery, ErrorOr<IReadOnlyList<StaffAvailabilityResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<StaffAvailabilityResponse>>> Handle(
        GetStaffAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(query.StaffMemberId, cancellationToken);
        if (staff is null)
            return StaffErrors.NotFound;

        var availabilities = await staffRepository.GetAvailabilitiesAsync(query.StaffMemberId, cancellationToken);

        return availabilities.Select(StaffAvailabilityResponse.FromEntity).ToList();
    }
}
