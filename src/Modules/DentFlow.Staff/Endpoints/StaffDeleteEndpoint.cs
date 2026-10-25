using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Staff.Endpoints;

public class StaffDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/staff/{id}");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s => s.Summary = "Soft-delete a staff profile with no appointments; 409 Staff.HasHistory otherwise");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new DeleteStaffMemberCommand(id, this.AccountActor()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
