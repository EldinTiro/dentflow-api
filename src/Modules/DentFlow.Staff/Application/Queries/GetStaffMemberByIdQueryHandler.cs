using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Queries;

public class GetStaffMemberByIdQueryHandler(IStaffRepository staffRepository, IUserAccountDirectory accounts)
    : IRequestHandler<GetStaffMemberByIdQuery, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(
        GetStaffMemberByIdQuery query,
        CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(query.Id, cancellationToken);
        if (staffMember is null)
            return StaffErrors.NotFound;

        if (staffMember.UserId is not { } userId)
            return StaffMemberResponse.FromEntity(staffMember, null);

        var summaries = await accounts.GetSummariesAsync([userId], cancellationToken);
        return StaffMemberResponse.FromEntity(staffMember, summaries.GetValueOrDefault(userId));
    }
}
