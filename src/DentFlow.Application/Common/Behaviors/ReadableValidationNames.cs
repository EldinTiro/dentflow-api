using System.Text.RegularExpressions;
using FluentValidation;

namespace DentFlow.Application.Common.Behaviors;

/// <summary>
/// DT-26: FastEndpoints installs a camel-case display name, so default messages read
/// "'first Name' must not be empty." in toasts. Messages use "First name" instead.
/// Call after <c>UseFastEndpoints</c>, which sets its own resolver.
/// </summary>
public static partial class ReadableValidationNames
{
    public static void Use() =>
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member is null ? null : Humanize(member.Name);

    public static string Humanize(string name)
    {
        var words = WordBoundary().Replace(name, " $1").Trim().ToLowerInvariant();
        return words.Length == 0 ? name : char.ToUpperInvariant(words[0]) + words[1..];
    }

    [GeneratedRegex("(?<=[a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
