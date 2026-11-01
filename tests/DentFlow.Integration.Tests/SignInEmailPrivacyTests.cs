using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-21: "A user with email … already exists." told one clinic that the address has an
/// account somewhere on DentFlow. The 409 stays, the wording no longer says that.
/// </summary>
[Collection("Integration")]
public class SignInEmailPrivacyTests(DentFlowAppFactory factory)
{
    [Fact]
    public async Task AnEmailUsedByAnotherClinicsAccount_IsRefusedWithoutSayingSo()
    {
        var email = $"taken-{Guid.NewGuid():N}@elsewhere.test";
        var otherTenant = await factory.SeedTenantAsync("privacy-other", "Other Clinic");
        await factory.SeedUserAsync(email, "Other123!@#abcd", Roles.Staff, otherTenant);

        var response = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "New", lastName = "Hire", email, access = new { role = Roles.Staff },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().ContainEquivalentOf("User.EmailAlreadyExists");
        body.Should().NotContain(email).And.NotContainEquivalentOf("already exists");
    }
}
