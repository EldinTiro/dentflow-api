using FluentValidation;

namespace DentFlow.Patients.Application.Commands;

/// <summary>
/// One upload limit for the whole stack (nginx sets client_max_body_size to match).
/// Phone photos over 1 MB used to hit nginx's default and fail with a bare 413.
/// </summary>
public static class DocumentLimits
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024;
}

public class UploadPatientDocumentCommandValidator : AbstractValidator<UploadPatientDocumentCommand>
{
    public UploadPatientDocumentCommandValidator()
    {
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .OverridePropertyName("Document.Empty");
        RuleFor(x => x.FileSizeBytes)
            .LessThanOrEqualTo(DocumentLimits.MaxFileSizeBytes).WithMessage("The file is larger than 20 MB.")
            // ValidationBehavior uses the property name as the error code
            .OverridePropertyName("Document.TooLarge");
    }
}
