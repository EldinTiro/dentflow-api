using System.Text.RegularExpressions;
using DentFlow.Domain.Identity;
using FluentAssertions;

namespace DentFlow.Architecture.Tests;

/// <summary>
/// DashboardStatsEndpoint allowed "Admin" and "OfficeManager" — roles no user can hold,
/// because hard-coded role strings drift away from <see cref="Roles"/>.
/// </summary>
public class RoleNameRules
{
    private static readonly Regex RolesCall = new(@"\bRoles\((?<args>[^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex Literal = new("\"(?<name>[^\"]+)\"", RegexOptions.Compiled);

    [Fact]
    public void EveryRoleNamedInAnEndpoint_ExistsInRoles()
    {
        var unknown = new List<string>();

        foreach (var file in Directory.EnumerateFiles(SourceTree.Modules, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            foreach (Match call in RolesCall.Matches(File.ReadAllText(file)))
                foreach (Match lit in Literal.Matches(call.Groups["args"].Value))
                    if (!Roles.All.Contains(lit.Groups["name"].Value))
                        unknown.Add($"{Path.GetFileName(file)}: {lit.Groups["name"].Value}");
        }

        unknown.Should().BeEmpty("a role string that is not in Roles.All can never be held by a user");
    }
}
