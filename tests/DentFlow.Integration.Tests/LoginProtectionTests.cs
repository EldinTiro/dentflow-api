using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-05: 15 wrong passwords in a row, then the right one worked — failures were never
/// counted. Now 5 failures lock the account for 15 minutes (Identity lockout), and each
/// client IP gets a small number of login attempts per minute.
/// </summary>
[Collection("Integration")]
public class LoginProtectionTests(DentFlowAppFactory factory)
{
    private const string Password = "Lock123!@#abcd";

    private async Task<(string Slug, string Email)> UserAsync()
    {
        var slug = $"lock-{Guid.NewGuid():N}"[..16];
        var tenantId = await factory.SeedTenantAsync(slug, "Lock Clinic");
        var email = $"lock-{Guid.NewGuid():N}@lock.test";
        await factory.SeedUserAsync(email, Password, Roles.Staff, tenantId);
        return (slug, email);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount_EvenForTheRightPassword()
    {
        var (slug, email) = await UserAsync();
        var client = factory.CreateAnonymousClient(slug);

        for (var i = 0; i < 5; i++)
            (await LoginAsync(client, email, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var locked = await LoginAsync(client, email, Password);

        locked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await locked.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Auth.AccountLocked");
    }

    [Fact]
    public async Task ASuccessfulLogin_ResetsTheFailureCount()
    {
        var (slug, email) = await UserAsync();
        var client = factory.CreateAnonymousClient(slug);

        for (var i = 0; i < 4; i++) await LoginAsync(client, email, "wrong-password");
        (await LoginAsync(client, email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        for (var i = 0; i < 4; i++) await LoginAsync(client, email, "wrong-password");

        (await LoginAsync(client, email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnknownEmail_StillAnswersTheSame401()
    {
        var (slug, _) = await UserAsync();

        var response = await LoginAsync(factory.CreateAnonymousClient(slug), "nobody@lock.test", Password);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TooManyLoginAttemptsFromOneClient_Answer429()
    {
        var (slug, email) = await UserAsync();
        // UseSetting, not ConfigureAppConfiguration: Program.cs reads the limit while building.
        using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:LoginPerMinute", "3"));
        var client = limited.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{slug}.localhost") });

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++) statuses.Add((await LoginAsync(client, email, "wrong-password")).StatusCode);

        statuses.Take(3).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        statuses[3].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
