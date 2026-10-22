using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Staff.Endpoints;

public class StaffDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/staff/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Soft-delete a staff member");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new DeleteStaffMemberCommand(id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
