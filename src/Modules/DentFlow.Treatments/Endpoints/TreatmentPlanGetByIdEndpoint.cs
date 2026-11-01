using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Treatments.Application;
using DentFlow.Treatments.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public class TreatmentPlanGetByIdEndpoint(ISender sender)
    : EndpointWithoutRequest<TreatmentPlanResponse>
{
    public override void Configure()
    {
        Get("/treatment-plans/{id}");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "Get a treatment plan by ID (includes all items)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var result = await sender.Send(new GetTreatmentPlanByIdQuery(id), ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
