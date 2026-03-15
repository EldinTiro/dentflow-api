using ErrorOr;
using MediatR;
using PearlDesk.Staff.Application.Interfaces;

namespace PearlDesk.Staff.Application.Queries;

public class ListStaffMembersQueryHandler(IStaffRepository staffRepository)
    : IRequestHandler<ListStaffMembersQuery, ErrorOr<PagedResult<StaffMemberResponse>>>
{
    public async Task<ErrorOr<PagedResult<StaffMemberResponse>>> Handle(
        ListStaffMembersQuery query,
        CancellationToken cancellationToken)
    {
        var (items, total) = await staffRepository.ListAsync(
            query.StaffType,
            query.IsActive,
            query.Page,
            query.PageSize,
            cancellationToken);

        var responses = items.Select(StaffMemberResponse.FromEntity).ToList();

        return new PagedResult<StaffMemberResponse>(responses, total, query.Page, query.PageSize);
    }
}

