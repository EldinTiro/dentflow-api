using FastEndpoints;
using MediatR;
using DentFlow.Treatments.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public class TreatmentPlanItemDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/treatment-plans/{planId}/items/{itemId}");
        Roles("ClinicOwner", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Remove an item from a treatment plan");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var itemId = Route<Guid>("itemId");

        var result = await sender.Send(new DeleteTreatmentPlanItemCommand(itemId), ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendNoContentAsync(ct);
    }
}
