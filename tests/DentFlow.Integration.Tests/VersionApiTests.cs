using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// The app shows which build is running (user request 2026-11-13): the API says its commit
/// and build time, written in by CI. Open to anyone: it tells nothing about a clinic.
/// </summary>
[Collection("Integration")]
public class VersionApiTests(DentFlowAppFactory factory)
{
    private record VersionDto(string? Commit, string? BuiltAt);

    [Fact]
    public async Task Version_SaysTheCommitAndBuildTimeWrittenInByTheBuild()
    {
        using var built = factory.WithWebHostBuilder(b => b
            .UseSetting("Build:Commit", "a1b2c3d4e5f6")
            .UseSetting("Build:Time", "2026-11-13T14:32:00Z"));
        var client = built.CreateClient();

        var r = await client.GetAsync("/api/v1/version");

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var v = (await r.Content.ReadFromJsonAsync<VersionDto>())!;
        v.Commit.Should().Be("a1b2c3d4e5f6");
        v.BuiltAt.Should().Be("2026-11-13T14:32:00Z");
    }

    [Fact]
    public async Task Version_WithoutABuild_SaysNothing()
    {
        var r = await factory.CreateClient().GetAsync("/api/v1/version");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var v = (await r.Content.ReadFromJsonAsync<VersionDto>())!;
        v.Commit.Should().BeNull();
    }
}
