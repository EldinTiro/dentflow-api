using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-17: turning off someone's sign-in blocked new logins, but their open session kept
/// working until the access token expired. Suspended clinics were already cut at once.
/// </summary>
[Collection("Integration")]
public class DeactivatedUserSessionTests(DentFlowAppFactory factory)
{
    private const string Password = "Sess123!@#abcd";
    private record LoginDto(string AccessToken, Guid UserId);

    private async Task<(HttpClient Client, Guid UserId)> SignedInAsync(string slug, string email)
    {
        var login = await factory.CreateAnonymousClient(slug).PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await login.Content.ReadFromJsonAsync<LoginDto>())!;
        var client = factory.CreateAnonymousClient(slug);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.AccessToken);
        return (client, dto.UserId);
    }

    [Fact]
    public async Task TurningOffSignIn_EndsTheOpenSessionAtOnce()
    {
        var slug = $"sess-{Guid.NewGuid():N}"[..16];
        var tenantId = await factory.SeedTenantAsync(slug, "Session Clinic");
        var ownerEmail = $"owner-{Guid.NewGuid():N}@sess.test";
        var nurseEmail = $"nurse-{Guid.NewGuid():N}@sess.test";
        await factory.SeedUserAsync(ownerEmail, Password, Roles.ClinicOwner, tenantId);
        await factory.SeedUserAsync(nurseEmail, Password, Roles.Staff, tenantId);
        var (owner, _) = await SignedInAsync(slug, ownerEmail);
        var (nurse, nurseId) = await SignedInAsync(slug, nurseEmail);
        (await nurse.GetAsync("/api/v1/patients")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await owner.PutAsJsonAsync($"/api/v1/users/{nurseId}/status", new { isActive = false })).IsSuccessStatusCode.Should().BeTrue();

        var after = await nurse.GetAsync("/api/v1/patients");
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await after.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Auth.AccountInactive");
        (await owner.GetAsync("/api/v1/patients")).StatusCode.Should().Be(HttpStatusCode.OK, "only that person is cut off");
    }
}
