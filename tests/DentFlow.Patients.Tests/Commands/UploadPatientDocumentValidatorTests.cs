using FluentAssertions;
using DentFlow.Patients.Application.Commands;

namespace DentFlow.Patients.Tests.Commands;

/// <summary>
/// Production report: phone photos over 1 MB were refused by the proxy with a bare 413.
/// The limit is now 20 MB, enforced here with a readable message, not left to the proxy.
/// </summary>
public class UploadPatientDocumentValidatorTests
{
    private static UploadPatientDocumentCommand Upload(long bytes) =>
        new(Guid.NewGuid(), "photo.jpg", "image/jpeg", bytes, Stream.Null, "Photo", null, Guid.NewGuid());

    [Theory]
    [InlineData(1)]
    [InlineData(1_700_000)]                 // the phone photo from the report
    [InlineData(20 * 1024 * 1024)]          // exactly 20 MB
    public void FilesUpTo20MB_AreAccepted(long bytes) =>
        new UploadPatientDocumentCommandValidator().Validate(Upload(bytes)).IsValid.Should().BeTrue();

    [Fact]
    public void AFileOver20MB_IsRefused_WithTheDocumentTooLargeCode()
    {
        var result = new UploadPatientDocumentCommandValidator().Validate(Upload(20 * 1024 * 1024 + 1));

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Document.TooLarge");
        result.Errors[0].ErrorMessage.Should().Be("The file is larger than 20 MB.");
    }

    [Fact]
    public void AnEmptyFile_IsRefused() =>
        new UploadPatientDocumentCommandValidator().Validate(Upload(0)).IsValid.Should().BeFalse();
}
