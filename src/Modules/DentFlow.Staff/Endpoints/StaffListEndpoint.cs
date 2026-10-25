using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Queries;
using DentFlow.Staff.Domain;
using DentFlow.Application.Common.Http;
using DomainRoles = DentFlow.Domain.Identity.Roles;

namespace DentFlow.Staff.Endpoints;

public class StaffListEndpoint(ISender sender) : EndpointWithoutRequest<PagedResult<StaffMemberResponse>>
{
    public override void Configure()
    {
        Get("/staff");
        Roles([.. DomainRoles.All]);
        Version(1);
        Summary(s => s.Summary = "List staff members with optional filters (default active; isActive=all for everyone)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var searchTerm = Query<string?>("searchTerm", isRequired: false);
        var staffTypeStr = Query<string?>("staffType", isRequired: false);
        // People-merge plan D11: active staff unless asked otherwise; "all" lifts the filter.
        var isActiveStr = Query<string?>("isActive", isRequired: false);
        bool? isActive = string.Equals(isActiveStr, "all", StringComparison.OrdinalIgnoreCase)
            ? null
            : bool.TryParse(isActiveStr, out var active) ? active : true;
        var pageStr = Query<string?>("page", isRequired: false);
        var pageSizeStr = Query<string?>("pageSize", isRequired: false);
        var page = int.TryParse(pageStr, out var p) ? p : 1;
        var pageSize = Math.Min(int.TryParse(pageSizeStr, out var ps) ? ps : 20, 100);

        StaffType? staffType = Enum.TryParse<StaffType>(staffTypeStr, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        var result = await sender.Send(new ListStaffMembersQuery(staffType, isActive, page, pageSize, searchTerm), ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}
