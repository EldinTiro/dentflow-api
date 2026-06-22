using ErrorOr;
using MediatR;
using DentFlow.Application.Common;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Features;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public class UploadPatientDocumentCommandHandler(
    IPatientRepository patientRepository,
    IPatientDocumentRepository documentRepository,
    IStorageService storageService,
    IFeatureService featureService)
    : IRequestHandler<UploadPatientDocumentCommand, ErrorOr<PatientDocumentResponse>>
{
    public async Task<ErrorOr<PatientDocumentResponse>> Handle(
        UploadPatientDocumentCommand command,
        CancellationToken cancellationToken)
    {
        if (!featureService.IsEnabled(FeatureFlag.DocumentStorage))
            return FeatureErrors.NotAvailableOnCurrentPlan;

        var patient = await patientRepository.GetByIdAsync(command.PatientId, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        var key = $"patients/{command.PatientId}/documents/{Guid.NewGuid()}/{command.FileName}";
        await storageService.UploadAsync(key, command.Content, command.ContentType, cancellationToken);

        var document = PatientDocument.Create(
            command.PatientId,
            command.FileName,
            command.ContentType,
            command.FileSizeBytes,
            key,
            command.Category,
            command.Notes,
            command.UploadedByUserId);

        await documentRepository.AddAsync(document, cancellationToken);

        return new PatientDocumentResponse(
            document.Id,
            document.PatientId,
            document.FileName,
            document.ContentType,
            document.FileSizeBytes,
            document.Category,
            document.Notes,
            document.UploadedByUserId,
            document.CreatedAt);
    }
}
