using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>QA-037: users could not edit their own name.</summary>
[Collection("Integration")]
public class OwnProfileTests(DentFlowAppFactory factory)
{
    private const string Password = "Profile123!@#abc";

    private record LoginDto(string AccessToken);
    private record ProfileDto(string FirstName, string LastName, string FullName);

    private async Task<HttpClient> SignedInAsync(string role)
    {
        var slug = $"prof-{Guid.NewGuid():N}"[..18];
        var tenantId = await factory.SeedTenantAsync(slug, "Profile Clinic");
        var email = $"{role.ToLowerInvariant()}-{slug}@test.com";
        await factory.SeedUserAsync(email, Password, role, tenantId);
        var client = factory.CreateAnonymousClient(slug);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken);
        return client;
    }

    [Fact]
    public async Task UpdateOwnName_IsSaved()
    {
        var client = await SignedInAsync(Roles.Receptionist);

        var response = await client.PutAsJsonAsync("/api/v1/users/me", new { firstName = "Amra", lastName = "Hadžić" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/v1/users/me");
        profile!.FullName.Should().Be("Amra Hadžić");
    }

    [Fact]
    public async Task UpdateOwnName_Empty_Returns400()
    {
        var client = await SignedInAsync(Roles.Receptionist);

        var response = await client.PutAsJsonAsync("/api/v1/users/me", new { firstName = " ", lastName = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    public async Task EveryRole_CanReadItsOwnProfileAndPreferences(string role)
    {
        var client = await SignedInAsync(role);

        (await client.GetAsync("/api/v1/users/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/users/me/preferences")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
