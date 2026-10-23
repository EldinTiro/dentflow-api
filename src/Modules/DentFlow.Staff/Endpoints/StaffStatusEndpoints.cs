using FastEndpoints;
using MediatR;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Commands;

namespace DentFlow.Staff.Endpoints;

/// <summary>POST /staff/{id}/deactivate — people-merge plan B6, instead of deleting.</summary>
public class StaffDeactivateEndpoint(ISender sender) : EndpointWithoutRequest<StaffMemberResponse>
{
    public override void Configure()
    {
        Post("/staff/{id}/deactivate");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Deactivate a staff member and their sign-in; 409 while appointments are still ahead");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new DeactivateStaffMemberCommand(Route<Guid>("id"), this.AccountActor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

/// <summary>POST /staff/{id}/reactivate — the sign-in stays off until access is given again.</summary>
public class StaffReactivateEndpoint(ISender sender) : EndpointWithoutRequest<StaffMemberResponse>
{
    public override void Configure()
    {
        Post("/staff/{id}/reactivate");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Reactivate a staff member; their sign-in stays inactive");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new ReactivateStaffMemberCommand(Route<Guid>("id")), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
