using System.Text.RegularExpressions;
using FluentAssertions;

namespace DentFlow.Architecture.Tests;

/// <summary>
/// DT-01 / DT-02: a request that takes a patient id must go through <c>PatientScopeBehavior</c>,
/// which only lets it run for a patient of the caller's clinic. Without the marker, the id was
/// stored as a foreign key — attaching rows to another clinic's patient or to no patient at all.
/// </summary>
public class PatientScopeRules
{
    // record Foo(…Guid PatientId…) : …IRequest<…>   (a non-nullable patient id)
    private static readonly Regex Request = new(
        @"public\s+record\s+(?<name>\w+)\s*\((?<args>[^;{]*?)\)\s*:\s*(?<bases>[^;{]*\bIRequest<)",
        RegexOptions.Singleline);

    [Fact]
    public void EveryRequestWithAPatientId_IsPatientScoped()
    {
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(SourceTree.Modules, "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match m in Request.Matches(File.ReadAllText(file)))
            {
                if (!Regex.IsMatch(m.Groups["args"].Value, @"\bGuid\s+PatientId\b")) continue;
                if (!m.Groups["bases"].Value.Contains("IPatientScoped"))
                    offenders.Add($"{Path.GetFileName(file)}: {m.Groups["name"].Value}");
            }
        }

        offenders.Should().BeEmpty("a request naming a patient must implement IPatientScoped");
    }
}
