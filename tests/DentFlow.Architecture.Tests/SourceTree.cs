using System.Runtime.CompilerServices;

namespace DentFlow.Architecture.Tests;

/// <summary>
/// Locates the source tree from this file's compile-time path, so the rules work no
/// matter where the test binaries are written.
/// </summary>
internal static class SourceTree
{
    public static string ApiRoot { get; } = Path.GetFullPath(Path.Combine(Here(), "..", "..", ".."));

    public static string Modules => Path.Combine(ApiRoot, "src", "Modules");

    private static string Here([CallerFilePath] string path = "") => path;
}
