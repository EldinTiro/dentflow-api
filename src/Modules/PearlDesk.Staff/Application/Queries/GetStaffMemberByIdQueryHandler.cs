using ErrorOr;
using MediatR;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Queries;

public class GetStaffMemberByIdQueryHandler(IStaffRepository staffRepository)
    : IRequestHandler<GetStaffMemberByIdQuery, ErrorOr<StaffMemberResponse>>
{
    public async Task<ErrorOr<StaffMemberResponse>> Handle(
        GetStaffMemberByIdQuery query,
        CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(query.Id, cancellationToken);
        if (staffMember is null)
            return StaffErrors.NotFound;

        return StaffMemberResponse.FromEntity(staffMember);
    }
}

