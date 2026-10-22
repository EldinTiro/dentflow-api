using FastEndpoints;
using MediatR;
using DentFlow.Treatments.Application;
using DentFlow.Treatments.Application.Commands;
using DentFlow.Treatments.Domain;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public record AddTreatmentPlanItemRequest(
    int? ToothNumber,
    string? Surface,
    string? CdtCode,
    string Description,
    decimal Fee,
    TreatmentPlanItemStatus Status);

public class TreatmentPlanItemAddEndpoint(ISender sender)
    : Endpoint<AddTreatmentPlanItemRequest, TreatmentPlanItemResponse>
{
    public override void Configure()
    {
        Post("/treatment-plans/{planId}/items");
        Roles("ClinicOwner", "ClinicAdmin", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Add an item to a treatment plan");
    }

    public override async Task HandleAsync(AddTreatmentPlanItemRequest req, CancellationToken ct)
    {
        var planId = Route<Guid>("planId");

        var command = new AddTreatmentPlanItemCommand(
            planId, req.ToothNumber, req.Surface, req.CdtCode,
            req.Description, req.Fee, req.Status);

        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendCreatedAtAsync<TreatmentPlanGetByIdEndpoint>(
            new { id = planId },
            result.Value,
            cancellation: ct);
    }
}
