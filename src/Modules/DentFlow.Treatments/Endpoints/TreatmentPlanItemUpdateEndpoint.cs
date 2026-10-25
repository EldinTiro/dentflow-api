using FastEndpoints;
using MediatR;
using DentFlow.Treatments.Application;
using DentFlow.Treatments.Application.Commands;
using DentFlow.Treatments.Domain;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public record UpdateTreatmentPlanItemRequest(
    int? ToothNumber,
    string? Surface,
    string? CdtCode,
    string Description,
    decimal Fee,
    TreatmentPlanItemStatus Status);

public class TreatmentPlanItemUpdateEndpoint(ISender sender)
    : Endpoint<UpdateTreatmentPlanItemRequest, TreatmentPlanItemResponse>
{
    public override void Configure()
    {
        Put("/treatment-plans/{planId}/items/{itemId}");
        Roles("ClinicOwner", "Dentist", "DentalNurse");
        Version(1);
        Summary(s => s.Summary = "Update a treatment plan item");
    }

    public override async Task HandleAsync(UpdateTreatmentPlanItemRequest req, CancellationToken ct)
    {
        var itemId = Route<Guid>("itemId");

        var command = new UpdateTreatmentPlanItemCommand(
            itemId, req.ToothNumber, req.Surface, req.CdtCode,
            req.Description, req.Fee, req.Status);

        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
