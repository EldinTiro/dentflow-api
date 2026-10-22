using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Patients.Endpoints;

public class PatientDocumentDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{patientId}/documents/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Delete a patient document");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var id = Route<Guid>("id");
        var result = await sender.Send(new DeletePatientDocumentCommand(patientId, id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
