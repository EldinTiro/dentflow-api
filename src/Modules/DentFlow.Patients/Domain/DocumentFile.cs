using System.Text;

namespace DentFlow.Patients.Domain;

/// <summary>
/// What a patient document may be, decided from the file itself (QA pass 2026-10-31, N-06/N-07).
/// The browser's content type and file name are only hints: an .exe, an HTML page and a
/// scripted SVG were stored, an HTML file sent as "application/pdf" was served as a PDF, and a
/// "../" file name went into the storage key. The allowed kinds match the Files tab's picker:
/// PDF, photos and scans, and Word documents.
/// </summary>
public static class DocumentFile
{
    /// <summary>How many leading bytes <see cref="DetectContentType"/> needs.</summary>
    public const int HeaderLength = 16;

    public const int MaxFileNameLength = 200;

    private const string FallbackName = "document";

    /// <summary>
    /// The content type of a supported file, read from its first bytes; <c>null</c> for
    /// anything else. Images and PDFs are recognised by their bytes alone (a JPEG named .png is
    /// still a JPEG). Zip and OLE2 containers are only accepted as Word documents, so the
    /// extension has to say so too.
    /// </summary>
    public static string? DetectContentType(ReadOnlySpan<byte> head, string fileName)
    {
        if (StartsWith(head, "%PDF-"u8)) return "application/pdf";
        if (StartsWith(head, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return "image/png";
        if (StartsWith(head, [0xFF, 0xD8, 0xFF])) return "image/jpeg";
        if (StartsWith(head, "GIF87a"u8) || StartsWith(head, "GIF89a"u8)) return "image/gif";
        if (StartsWith(head, "RIFF"u8) && head.Length >= 12 && head[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        if (IsHeic(head)) return "image/heic";

        var extension = Path.GetExtension(SafeFileName(fileName)).ToLowerInvariant();
        if (StartsWith(head, [0x50, 0x4B, 0x03, 0x04]) && extension == ".docx")
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        if (StartsWith(head, [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]) && extension == ".doc")
            return "application/msword";

        return null;
    }

    /// <summary>
    /// The last path segment of a client file name, without control characters or leading
    /// dots, shortened to <see cref="MaxFileNameLength"/> with its extension kept.
    /// </summary>
    public static string SafeFileName(string fileName)
    {
        var lastSegment = (fileName ?? string.Empty).Replace('\\', '/').Split('/').Last();
        var cleaned = new StringBuilder(lastSegment.Length);
        foreach (var c in lastSegment)
            if (!char.IsControl(c)) cleaned.Append(c);

        var name = cleaned.ToString().Trim().TrimStart('.').Trim();
        if (name.Length == 0) return FallbackName;
        if (name.Length <= MaxFileNameLength) return name;

        var extension = Path.GetExtension(name);
        if (extension.Length >= MaxFileNameLength / 2) extension = string.Empty;
        return name[..(MaxFileNameLength - extension.Length)] + extension;
    }

    private static bool StartsWith(ReadOnlySpan<byte> head, ReadOnlySpan<byte> signature) =>
        head.Length >= signature.Length && head[..signature.Length].SequenceEqual(signature);

    private static bool IsHeic(ReadOnlySpan<byte> head)
    {
        if (head.Length < 12 || !head[4..8].SequenceEqual("ftyp"u8)) return false;
        var brand = Encoding.ASCII.GetString(head[8..12]);
        return brand is "heic" or "heix" or "heim" or "heis" or "hevc" or "hevx" or "mif1" or "msf1";
    }
}

public static class DocumentErrors
{
    public static readonly ErrorOr.Error UnsupportedType = ErrorOr.Error.Validation(
        "Document.UnsupportedType",
        "This kind of file can't be uploaded. Use a PDF, a photo (JPG, PNG, WEBP, HEIC) or a Word document.");
}
