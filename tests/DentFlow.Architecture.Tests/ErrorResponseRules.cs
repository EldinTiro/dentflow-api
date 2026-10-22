using System.Text.RegularExpressions;
using DentFlow.Application.Common.Http;
using ErrorOr;
using FluentAssertions;

namespace DentFlow.Architecture.Tests;

/// <summary>
/// QA-005: 50 endpoints answered <c>result.IsError</c> with a bare <c>SendErrorsAsync()</c>,
/// discarding the errors and collapsing every status to 400.
/// </summary>
public class ErrorResponseRules
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Failure, 400)]
    [InlineData(ErrorType.Unexpected, 500)]
    public void ToStatusCode_MapsEachErrorType(ErrorType type, int expected) =>
        type.ToStatusCode().Should().Be(expected);

    // Endpoints with a deliberate, documented non-standard error response.
    private static readonly string[] Allowed =
    [
        "RefreshEndpoint.cs", // clears the refresh cookie and answers 401
    ];

    [Fact]
    public void EveryIsErrorBranch_SendsTheResultErrors()
    {
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(SourceTree.Modules, "*.cs", SearchOption.AllDirectories))
        {
            if (Allowed.Contains(Path.GetFileName(file))) continue;
            var source = File.ReadAllText(file);

            foreach (Match m in Regex.Matches(source, @"if \((\w+)\.IsError\)\s*\{"))
            {
                var block = BlockAt(source, m.Index + m.Length);
                if (!block.Contains("SendResultErrorsAsync("))
                    offenders.Add($"{Path.GetRelativePath(SourceTree.ApiRoot, file)}: {Regex.Replace(block, @"\s+", " ").Trim()}");
            }
        }

        Directory.Exists(SourceTree.Modules).Should().BeTrue("the rule must actually scan something");
        offenders.Should().BeEmpty("every IsError branch must call this.SendResultErrorsAsync(result.Errors, ct)");
    }

    private static string BlockAt(string source, int afterOpenBrace)
    {
        var depth = 1;
        var i = afterOpenBrace;
        while (depth > 0 && i < source.Length)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}') depth--;
            i++;
        }
        return source[afterOpenBrace..(i - 1)];
    }
}
