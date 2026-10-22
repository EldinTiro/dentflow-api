using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-002: deactivating a clinic must suspend it — no new logins, no refreshes, and
/// access tokens issued before the deactivation stop working.
/// Each test uses its own clinic so deactivation cannot leak into other tests.
/// </summary>
[Collection("Integration")]
public class SuspendedTenantTests(DentFlowAppFactory factory)
{
    private const string Password = "Suspended123!@#";

    private async Task<(Guid TenantId, string Slug, string Email)> ClinicWithOwnerAsync()
    {
        var slug = $"susp-{Guid.NewGuid():N}"[..20];
        var tenantId = await factory.SeedTenantAsync(slug, "Suspended Clinic");
        var email = $"{slug}@test.com";
        await factory.SeedUserAsync(email, Password, "ClinicOwner", tenantId);
        return (tenantId, slug, email);
    }

    private async Task SetActiveAsync(Guid tenantId, bool active)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = await db.Tenants.SingleAsync(t => t.Id == tenantId);
        if (active) tenant.Activate(); else tenant.Deactivate();
        await db.SaveChangesAsync();
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

    private static string RefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase))
            ["refresh_token=".Length..].Split(';')[0];

    [Fact]
    public async Task Login_UserOfDeactivatedClinic_Returns403WithReason()
    {
        var (tenantId, slug, email) = await ClinicWithOwnerAsync();
        await SetActiveAsync(tenantId, false);

        var response = await LoginAsync(factory.CreateAnonymousClient(slug), email);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().Contain("suspended");
    }

    [Fact]
    public async Task Refresh_AfterClinicDeactivated_Returns401()
    {
        var (tenantId, slug, email) = await ClinicWithOwnerAsync();
        var client = factory.CreateAnonymousClient(slug);
        var login = await LoginAsync(client, email);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        await SetActiveAsync(tenantId, false);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={RefreshCookie(login)}");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExistingAccessToken_AfterClinicDeactivated_Returns403()
    {
        var (tenantId, slug, _) = await ClinicWithOwnerAsync();
        var client = factory.CreateClientForTenant(tenantId, slug);
        (await client.GetAsync("/api/v1/patients")).StatusCode.Should().Be(HttpStatusCode.OK);

        await SetActiveAsync(tenantId, false);

        (await client.GetAsync("/api/v1/patients")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReactivatedClinic_CanLogInAgain()
    {
        var (tenantId, slug, email) = await ClinicWithOwnerAsync();
        await SetActiveAsync(tenantId, false);
        await SetActiveAsync(tenantId, true);

        var response = await LoginAsync(factory.CreateAnonymousClient(slug), email);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
