using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Patients.Endpoints;

public class PatientDocumentListEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<PatientDocumentResponse>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/documents");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "List documents for a patient");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(new ListPatientDocumentsQuery(patientId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
