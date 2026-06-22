using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;

namespace DentFlow.Staff.Application.Queries;

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
            query.SearchTerm,
            cancellationToken);

        var responses = items.Select(StaffMemberResponse.FromEntity).ToList();

        return new PagedResult<StaffMemberResponse>(responses, total, query.Page, query.PageSize);
    }
}

