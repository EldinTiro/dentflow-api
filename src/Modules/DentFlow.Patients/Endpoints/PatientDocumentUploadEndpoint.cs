using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http;
using DentFlow.Patients.Application.Commands;

namespace DentFlow.Patients.Endpoints;

public class PatientDocumentUploadRequest
{
    public IFormFile File { get; set; } = default!;
    public string Category { get; set; } = "Other";
    public string? Notes { get; set; }
}

public class PatientDocumentUploadEndpoint(ISender sender)
    : Endpoint<PatientDocumentUploadRequest, Application.PatientDocumentResponse>
{
    public override void Configure()
    {
        Post("/patients/{patientId}/documents");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        AllowFileUploads();
        Summary(s => s.Summary = "Upload a document for a patient");
    }

    public override async Task HandleAsync(PatientDocumentUploadRequest req, CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        await using var stream = req.File.OpenReadStream();
        var command = new UploadPatientDocumentCommand(
            patientId,
            req.File.FileName,
            req.File.ContentType,
            req.File.Length,
            stream,
            req.Category,
            req.Notes,
            userId);

        var result = await sender.Send(command, ct);
        if (result.IsError)
        {
            foreach (var e in result.Errors)
                AddError(e.Description);
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        await SendCreatedAtAsync<PatientDocumentListEndpoint>(
            new { patientId }, result.Value, cancellation: ct);
    }
}
