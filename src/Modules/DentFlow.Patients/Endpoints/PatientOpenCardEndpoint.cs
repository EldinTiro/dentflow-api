using FastEndpoints;
using MediatR;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;

namespace DentFlow.Patients.Endpoints;

public record OpenPatientCardRequest(string? PatientNumber);

/// <summary>POST /patients/{id}/card — open the card of a patient saved without one.</summary>
public class PatientOpenCardEndpoint(ISender sender) : Endpoint<OpenPatientCardRequest, PatientResponse>
{
    public override void Configure()
    {
        Post("/patients/{id}/card");
        Policies(Permission.PatientsManage);
        Version(1);
        Summary(s => s.Summary = "Open a patient's card: the given number, or the clinic's next one; 409 if already open");
    }

    public override async Task HandleAsync(OpenPatientCardRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new OpenPatientCardCommand(Route<Guid>("id"), req.PatientNumber), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
