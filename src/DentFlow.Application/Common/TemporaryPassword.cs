namespace DentFlow.Application.Common;

/// <summary>
/// A one-time password handed to someone who must replace it at first sign-in
/// (MustChangePassword). Satisfies the Identity policy: upper, lower, digit, symbol, length.
/// </summary>
public static class TemporaryPassword
{
    public static string Generate() => $"Tmp_{Guid.NewGuid():N}!A1";
}
