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

        // N-06/N-07: the type comes from the file's own bytes and the name is cleaned, so the
        // browser's claims never decide what is stored or how it is served.
        var content = await SeekableAsync(command.Content, cancellationToken);
        var head = new byte[DocumentFile.HeaderLength];
        var read = await content.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;

        var contentType = DocumentFile.DetectContentType(head.AsSpan(0, read), command.FileName);
        if (contentType is null)
            return DocumentErrors.UnsupportedType;

        var fileName = DocumentFile.SafeFileName(command.FileName);
        var key = $"patients/{command.PatientId}/documents/{Guid.NewGuid()}/{fileName}";
        await storageService.UploadAsync(key, content, contentType, cancellationToken);

        var document = PatientDocument.Create(
            command.PatientId,
            fileName,
            contentType,
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

    /// <summary>The upload stream rewound after sniffing; buffered first if it can't seek (at most 20 MB).</summary>
    private static async Task<Stream> SeekableAsync(Stream stream, CancellationToken ct)
    {
        if (stream.CanSeek) return stream;
        var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return buffer;
    }
}
