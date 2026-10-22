using FastEndpoints;
using MediatR;
using DentFlow.Treatments.Application;
using DentFlow.Treatments.Application.Commands;
using DentFlow.Treatments.Domain;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public record UpdateTreatmentPlanRequest(
    string Title,
    string? Notes,
    TreatmentPlanStatus Status);

public class TreatmentPlanUpdateEndpoint(ISender sender)
    : Endpoint<UpdateTreatmentPlanRequest, TreatmentPlanResponse>
{
    public override void Configure()
    {
        Put("/treatment-plans/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Update a treatment plan");
    }

    public override async Task HandleAsync(UpdateTreatmentPlanRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");

        var command = new UpdateTreatmentPlanCommand(id, req.Title, req.Notes, req.Status);
        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
