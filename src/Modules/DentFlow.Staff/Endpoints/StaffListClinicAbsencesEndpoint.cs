using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Queries;
using DentFlow.Application.Common.Http;
using DomainRoles = DentFlow.Domain.Identity.Roles;

namespace DentFlow.Staff.Endpoints;

public class StaffListClinicAbsencesEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<StaffBlockedTimeResponse>>
{
    public override void Configure()
    {
        Get("/staff/blocked-times");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s =>
        {
            s.Summary = "List every staff absence that overlaps a range";
            s.Description = "'from' and 'to' are UTC instants (local day boundaries). Used by the dashboard's 'who is away'.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var from = Query<DateTime>("from");
        var to = Query<DateTime>("to");

        var result = await sender.Send(new ListClinicAbsencesQuery(from.ToUniversalTime(), to.ToUniversalTime()), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
