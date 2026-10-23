using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Patients.Endpoints;

public record PatientDocumentUrlResponse(string Url);

public class PatientDocumentGetUrlEndpoint(ISender sender) : EndpointWithoutRequest<PatientDocumentUrlResponse>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/documents/{id}/url");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get a presigned download URL for a patient document");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetPatientDocumentUrlQuery(patientId, id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(new PatientDocumentUrlResponse(result.Value), ct);
    }
}
