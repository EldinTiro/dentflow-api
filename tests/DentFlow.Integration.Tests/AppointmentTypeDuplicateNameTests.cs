using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>QA pass 2026-10-31 (N-11): a second service type with a used name was a 500.</summary>
[Collection("Integration")]
public class AppointmentTypeDuplicateNameTests(DentFlowAppFactory factory)
{
    private static object Type(string name) => new { name, defaultDurationMinutes = 30, isBookableOnline = false };

    [Fact]
    public async Task ANameTheClinicAlreadyUses_Is409_EvenInOtherCase()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner], "dup-type-clinic");
        (await client.PostAsJsonAsync("/api/v1/appointment-types", Type("Whitening"))).StatusCode.Should().Be(HttpStatusCode.Created);

        var same = await client.PostAsJsonAsync("/api/v1/appointment-types", Type("Whitening"));
        var otherCase = await client.PostAsJsonAsync("/api/v1/appointment-types", Type(" whitening "));

        same.StatusCode.Should().Be(HttpStatusCode.Conflict, await same.Content.ReadAsStringAsync());
        (await same.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("AppointmentType.DuplicateName");
        otherCase.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RenamingATypeToAnotherTypesName_Is409_ButSavingItsOwnNameIsFine()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner], "dup-type-rename");
        await client.PostAsJsonAsync("/api/v1/appointment-types", Type("Bleaching"));
        var created = await client.PostAsJsonAsync("/api/v1/appointment-types", Type("Polishing"));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var clash = await client.PutAsJsonAsync($"/api/v1/appointment-types/{id}", Type("Bleaching"));
        var own = await client.PutAsJsonAsync($"/api/v1/appointment-types/{id}", Type("Polishing"));

        clash.StatusCode.Should().Be(HttpStatusCode.Conflict);
        own.StatusCode.Should().Be(HttpStatusCode.OK, await own.Content.ReadAsStringAsync());
    }
}
