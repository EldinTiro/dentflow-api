using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Staff.Endpoints;

public class StaffPurgeEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/staff/{id}/permanent");
        Policies(Permission.RecordsPurge);
        Version(1);
        Summary(s => s.Summary = "Delete a staff profile with no appointments for good, with its sign-in; 409 Staff.HasHistory otherwise (Administrator)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new PurgeStaffMemberCommand(Route<Guid>("id"), this.AccountActor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
