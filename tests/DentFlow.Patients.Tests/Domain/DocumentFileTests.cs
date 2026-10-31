using System.Text;
using FluentAssertions;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Tests;

/// <summary>
/// QA pass 2026-10-31 (N-06, N-07): uploads trusted the browser's content type and file name.
/// An .exe, an .html page and a scripted .svg were stored, an HTML file sent as
/// "application/pdf" was stored as a PDF and later served as one, and a file name with "../"
/// went straight into the storage key (500 from S3). The type now comes from the file's own
/// first bytes, and the name is reduced to a plain file name.
/// </summary>
public class DocumentFileTests
{
    private static byte[] Bytes(params int[] b) => b.Select(x => (byte)x).ToArray();
    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);
    private static byte[] With(byte[] head, int length = 64) => head.Concat(new byte[Math.Max(0, length - head.Length)]).ToArray();

    [Theory]
    [InlineData("scan.pdf")]
    [InlineData("scan.PDF")]
    [InlineData("scan")]
    public void APdf_IsRecognisedByItsBytes(string name) =>
        DocumentFile.DetectContentType(With(Ascii("%PDF-1.7")), name).Should().Be("application/pdf");

    [Fact]
    public void APng_IsRecognised() =>
        DocumentFile.DetectContentType(With(Bytes(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)), "xray.png")
            .Should().Be("image/png");

    [Fact]
    public void AJpeg_IsRecognised_EvenWithAnotherExtension() =>
        DocumentFile.DetectContentType(With(Bytes(0xFF, 0xD8, 0xFF, 0xE0)), "photo.png")
            .Should().Be("image/jpeg");

    [Fact]
    public void AWebp_IsRecognised() =>
        DocumentFile.DetectContentType(With(Ascii("RIFF\0\0\0\0WEBPVP8 ")), "photo.webp").Should().Be("image/webp");

    [Fact]
    public void AnIphoneHeicPhoto_IsRecognised() =>
        DocumentFile.DetectContentType(With(Ascii("\0\0\0\u0018ftypheic")), "IMG_0001.HEIC").Should().Be("image/heic");

    [Fact]
    public void AWordDocx_IsRecognised_ByZipBytesAndExtension() =>
        DocumentFile.DetectContentType(With(Bytes(0x50, 0x4B, 0x03, 0x04)), "consent.docx")
            .Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

    [Fact]
    public void AnOldWordDoc_IsRecognised() =>
        DocumentFile.DetectContentType(With(Bytes(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1)), "consent.doc")
            .Should().Be("application/msword");

    [Theory]
    [InlineData("<script>alert(1)</script>", "evil.html")]
    [InlineData("<script>alert(1)</script>", "evil.pdf")]                         // HTML pretending to be a PDF
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"/>", "evil.svg")]
    [InlineData("MZ\u0090\0", "setup.exe")]
    [InlineData("PK\u0003\u0004", "payload.zip")]                                 // a zip that is not a .docx
    [InlineData("", "empty.pdf")]
    public void AnythingElse_IsRefused(string content, string name) =>
        DocumentFile.DetectContentType(Ascii(content), name).Should().BeNull();

    [Theory]
    [InlineData("../../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("..\\..\\windows\\scan.pdf", "scan.pdf")]
    [InlineData("C:\\Users\\me\\Desktop\\x-ray 1.png", "x-ray 1.png")]
    [InlineData("rendgen čćž.jpg", "rendgen čćž.jpg")]
    [InlineData("..", "document")]
    [InlineData("   ", "document")]
    [InlineData(".hidden.pdf", "hidden.pdf")]
    [InlineData("a\u0000b.pdf", "ab.pdf")]
    public void AFileName_IsReducedToAPlainName(string given, string expected) =>
        DocumentFile.SafeFileName(given).Should().Be(expected);

    [Fact]
    public void AVeryLongFileName_IsShortenedButKeepsItsExtension()
    {
        var safe = DocumentFile.SafeFileName(new string('a', 400) + ".pdf");

        safe.Length.Should().BeLessThanOrEqualTo(DocumentFile.MaxFileNameLength);
        safe.Should().EndWith(".pdf");
    }
}
