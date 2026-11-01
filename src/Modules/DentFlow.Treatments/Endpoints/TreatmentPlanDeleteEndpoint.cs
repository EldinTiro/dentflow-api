using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Treatments.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public class TreatmentPlanDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/treatment-plans/{id}");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "Soft-delete a treatment plan");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var result = await sender.Send(new DeleteTreatmentPlanCommand(id), ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendNoContentAsync(ct);
    }
}
