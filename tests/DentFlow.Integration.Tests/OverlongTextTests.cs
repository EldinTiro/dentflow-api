using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA pass 2026-10-31 (N-01): text longer than its database column (patient city, allergen,
/// staff specialty, payment reference…) reached PostgreSQL and came back as a 500
/// "value too long for type character varying". It is the caller's input, so it is a 400
/// with a readable reason, on every endpoint — not only the ones that have a length rule.
/// </summary>
[Collection("Integration")]
public class OverlongTextTests(DentFlowAppFactory factory)
{
    private static readonly string TooLong = new('x', 3000);

    private async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/patients", new { firstName = "Long", lastName = "Text" });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task ShouldBeReadable400(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("Validation.TooLong");
    }

    [Fact]
    public async Task APatientCityLongerThanItsColumn_Is400_NotA500()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);

        var response = await client.PostAsJsonAsync("/api/v1/patients",
            new { firstName = "Long", lastName = "City", city = TooLong });

        await ShouldBeReadable400(response);
    }

    [Fact]
    public async Task AnAllergenLongerThanItsColumn_Is400_NotA500()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/allergies", new { allergen = TooLong });

        await ShouldBeReadable400(response);
    }

    [Fact]
    public async Task AStaffSpecialtyLongerThanItsColumn_Is400_NotA500()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);

        var response = await client.PostAsJsonAsync("/api/v1/staff",
            new { staffType = "Receptionist", firstName = "Long", lastName = "Specialty", specialty = TooLong });

        await ShouldBeReadable400(response);
    }

    [Fact]
    public async Task AToothFindingNoteLongerThanItsColumn_Is400_NotA500()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await CreatePatientAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/teeth",
            new { tooth = 11, condition = "Crown", notes = TooLong });

        await ShouldBeReadable400(response);
    }

    [Fact]
    public async Task NothingIsStored_WhenTheTextIsTooLong()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await CreatePatientAsync(client);

        await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/allergies", new { allergen = TooLong });

        var allergies = await client.GetFromJsonAsync<JsonElement>($"/api/v1/patients/{patientId}/allergies");
        allergies.GetArrayLength().Should().Be(0);
    }
}
