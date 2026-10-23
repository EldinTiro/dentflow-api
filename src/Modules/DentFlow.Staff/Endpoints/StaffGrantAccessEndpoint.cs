using FastEndpoints;
using MediatR;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;
using DentFlow.Staff.Application.Commands;

namespace DentFlow.Staff.Endpoints;

public record GrantStaffAccessRequest(string Role);

/// <summary>POST /staff/{id}/access — the person can now sign in (people-merge plan 2.2).</summary>
public class StaffGrantAccessEndpoint(ISender sender) : Endpoint<GrantStaffAccessRequest, GrantStaffAccessResult>
{
    public override void Configure()
    {
        Post("/staff/{id}/access");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Give a staff member a sign-in account; returns a one-time temporary password");
    }

    public override async Task HandleAsync(GrantStaffAccessRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new GrantStaffAccessCommand(Route<Guid>("id"), req.Role), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
