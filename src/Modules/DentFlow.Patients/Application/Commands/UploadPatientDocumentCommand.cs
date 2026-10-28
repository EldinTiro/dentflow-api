using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

public record UploadPatientDocumentCommand(
    Guid PatientId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content,
    string Category,
    string? Notes,
    Guid UploadedByUserId) : IPatientScoped, IRequest<ErrorOr<PatientDocumentResponse>>;
