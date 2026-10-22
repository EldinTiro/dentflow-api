using FastEndpoints;
using MediatR;
using DentFlow.Treatments.Application;
using DentFlow.Treatments.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public record CreateTreatmentPlanRequest(
    string Title,
    string? Notes);

public class TreatmentPlanCreateEndpoint(ISender sender)
    : Endpoint<CreateTreatmentPlanRequest, TreatmentPlanResponse>
{
    public override void Configure()
    {
        Post("/patients/{patientId}/treatment-plans");
        Roles("ClinicOwner", "ClinicAdmin", "Dentist", "Hygienist", "Receptionist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Create a new treatment plan for a patient");
    }

    public override async Task HandleAsync(CreateTreatmentPlanRequest req, CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");

        var command = new CreateTreatmentPlanCommand(patientId, req.Title, req.Notes);
        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendCreatedAtAsync<TreatmentPlanGetByIdEndpoint>(
            new { id = result.Value.Id },
            result.Value,
            cancellation: ct);
    }
}
