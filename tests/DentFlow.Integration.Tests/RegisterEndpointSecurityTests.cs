using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// POST /auth/register was [AllowAnonymous] ("temporarily open") and accepted any role,
/// SuperAdmin included: anyone who could reach the API could create a platform admin.
/// Clinic staff are created through the Team invite flow; this endpoint is SuperAdmin-only.
/// </summary>
[Collection("Integration")]
public class RegisterEndpointSecurityTests(DentFlowAppFactory factory)
{
    private static object NewSuperAdmin() => new
    {
        email = $"attacker-{Guid.NewGuid():N}@evil.test",
        password = "Str0ng!Passw0rd",
        firstName = "Not",
        lastName = "Allowed",
        role = "SuperAdmin",
    };

    [Fact]
    public async Task Register_Anonymous_IsUnauthorized()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/v1/auth/register", NewSuperAdmin());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_ClinicStaff_IsForbidden()
    {
        var owner = factory.CreateAuthenticatedClient(["ClinicOwner"]);

        var response = await owner.PostAsJsonAsync("/api/v1/auth/register", NewSuperAdmin());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
