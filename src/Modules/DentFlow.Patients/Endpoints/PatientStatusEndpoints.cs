using FastEndpoints;
using MediatR;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;

namespace DentFlow.Patients.Endpoints;

public record DeactivatePatientRequest(string? Reason);

/// <summary>POST /patients/{id}/deactivate — people-merge plan D8: instead of deleting.</summary>
public class PatientDeactivateEndpoint(ISender sender) : Endpoint<DeactivatePatientRequest, PatientResponse>
{
    public override void Configure()
    {
        Post("/patients/{id}/deactivate");
        Policies(Permission.PatientsManage);
        Version(1);
        Summary(s => s.Summary = "Deactivate a patient; 409 while appointments are still ahead");
    }

    public override async Task HandleAsync(DeactivatePatientRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new DeactivatePatientCommand(Route<Guid>("id"), req.Reason), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

/// <summary>POST /patients/{id}/reactivate</summary>
public class PatientReactivateEndpoint(ISender sender) : EndpointWithoutRequest<PatientResponse>
{
    public override void Configure()
    {
        Post("/patients/{id}/reactivate");
        Policies(Permission.PatientsManage);
        Version(1);
        Summary(s => s.Summary = "Reactivate an inactive or transferred patient");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new ReactivatePatientCommand(Route<Guid>("id")), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
