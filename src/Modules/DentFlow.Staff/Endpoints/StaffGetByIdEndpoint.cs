using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Queries;
using DentFlow.Application.Common.Http;
using DomainRoles = DentFlow.Domain.Identity.Roles;

namespace DentFlow.Staff.Endpoints;

public class StaffGetByIdEndpoint(ISender sender) : EndpointWithoutRequest<StaffMemberResponse>
{
    public override void Configure()
    {
        Get("/staff/{id}");
        // Clinic roles only; the platform admin has no clinic data (DT-18).
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "Get a staff member by ID");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetStaffMemberByIdQuery(id, this.Can(DentFlow.Domain.Identity.Permission.PeopleManage)), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
