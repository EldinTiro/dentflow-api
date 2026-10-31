using System.Text;
using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Features;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Tests.Commands;

/// <summary>
/// QA pass 2026-10-31 (N-06, N-07): what reaches storage is decided by the file's bytes and a
/// cleaned name, never by the browser's content type or a "../" file name.
/// </summary>
public class UploadPatientDocumentCommandHandlerTests
{
    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IPatientDocumentRepository _documents = Substitute.For<IPatientDocumentRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IFeatureService _features = Substitute.For<IFeatureService>();
    private readonly Patient _patient = Patient.Create("P-000042", "Up", "Load", null, null, null);

    public UploadPatientDocumentCommandHandlerTests()
    {
        _features.IsEnabled(Arg.Any<FeatureFlag>()).Returns(true);
        _patients.GetByIdAsync(_patient.Id, Arg.Any<CancellationToken>()).Returns(_patient);
    }

    private UploadPatientDocumentCommandHandler Sut() => new(_patients, _documents, _storage, _features);

    private UploadPatientDocumentCommand Upload(string content, string fileName, string claimedType) =>
        new(_patient.Id, fileName, claimedType, content.Length, new MemoryStream(Encoding.ASCII.GetBytes(content)),
            "Other", null, Guid.NewGuid());

    [Fact]
    public async Task Handle_HtmlClaimingToBeAPdf_IsRefused_AndNothingIsStored()
    {
        var result = await Sut().Handle(Upload("<script>alert(1)</script>", "evil.pdf", "application/pdf"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Document.UnsupportedType");
        await _storage.DidNotReceive().UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _documents.DidNotReceive().AddAsync(Arg.Any<PatientDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_APdf_IsStoredAsAPdf_WhateverTheBrowserClaimed()
    {
        var result = await Sut().Handle(Upload("%PDF-1.7 rest", "scan.pdf", "text/html"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.ContentType.Should().Be("application/pdf");
        await _storage.Received(1).UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AFileNameWithDotDot_IsCleaned_BeforeItReachesTheStorageKey()
    {
        var result = await Sut().Handle(Upload("%PDF-1.7 rest", "../../../etc/passwd.pdf", "application/pdf"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.FileName.Should().Be("passwd.pdf");
        await _storage.Received(1).UploadAsync(
            Arg.Is<string>(key => !key.Contains("..") && key.EndsWith("/passwd.pdf")),
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TheWholeFileIsUploaded_NotOnlyWhatWasSniffed()
    {
        var content = "%PDF-1.7 " + new string('x', 5000);
        long uploadedLength = 0;
        await _storage.UploadAsync(Arg.Any<string>(), Arg.Do<Stream>(s => uploadedLength = s.Length - s.Position), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await Sut().Handle(Upload(content, "big.pdf", "application/pdf"), CancellationToken.None);

        uploadedLength.Should().Be(content.Length);
    }
}
