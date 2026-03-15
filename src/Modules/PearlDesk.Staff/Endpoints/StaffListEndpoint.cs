using FastEndpoints;
using MediatR;
using PearlDesk.Staff.Application;
using PearlDesk.Staff.Application.Queries;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Endpoints;

public class StaffListEndpoint(ISender sender) : EndpointWithoutRequest<PagedResult<StaffMemberResponse>>
{
    public override void Configure()
    {
        Get("/staff");
        Roles("ClinicOwner", "ClinicAdmin", "Dentist", "Hygienist", "Receptionist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "List staff members with optional filters");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var staffTypeStr = Query<string?>("staffType", isRequired: false);
        var isActive = Query<bool?>("isActive", isRequired: false);
        var pageStr = Query<string?>("page", isRequired: false);
        var pageSizeStr = Query<string?>("pageSize", isRequired: false);
        var page = int.TryParse(pageStr, out var p) ? p : 1;
        var pageSize = Math.Min(int.TryParse(pageSizeStr, out var ps) ? ps : 20, 100);

        StaffType? staffType = Enum.TryParse<StaffType>(staffTypeStr, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        var result = await sender.Send(new ListStaffMembersQuery(staffType, isActive, page, pageSize), ct);

        if (result.IsError)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}
