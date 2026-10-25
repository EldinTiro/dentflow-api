using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Interfaces;

namespace DentFlow.Staff.Application.Queries;

public class ListStaffMembersQueryHandler(IStaffRepository staffRepository, IUserAccountDirectory accounts)
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

        // One account lookup for the page, not one per row.
        var linked = items.Where(s => s.UserId is not null).Select(s => s.UserId!.Value).ToList();
        var summaries = await accounts.GetSummariesAsync(linked, cancellationToken);
        var responses = items
            .Select(s => StaffMemberResponse.FromEntity(s, s.UserId is { } id ? summaries.GetValueOrDefault(id) : null))
            .ToList();

        return new PagedResult<StaffMemberResponse>(responses, total, query.Page, query.PageSize);
    }
}

