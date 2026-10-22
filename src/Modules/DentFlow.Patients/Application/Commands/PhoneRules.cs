using FluentValidation;

namespace DentFlow.Patients.Application.Commands;

/// <summary>
/// A phone number has 8-15 digits, optionally a leading "+", and may be written with
/// spaces, dashes or parentheses. "+387" + "12" used to be saved as a mobile (QA-019).
/// </summary>
internal static class PhoneRules
{
    public const string Message = "Enter a valid phone number, e.g. +387 61 123 456.";

    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(BeAPlausibleNumber).WithMessage(Message);

    private static bool BeAPlausibleNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var compact = phone.Trim();
        if (compact.StartsWith('+')) compact = compact[1..];
        if (compact.Any(c => !char.IsDigit(c) && c is not (' ' or '-' or '(' or ')'))) return false;
        var digits = compact.Count(char.IsDigit);
        return digits is >= 8 and <= 15;
    }
}
