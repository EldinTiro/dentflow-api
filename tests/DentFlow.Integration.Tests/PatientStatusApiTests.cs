using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>People-merge plan B2: deactivate / reactivate over HTTP.</summary>
[Collection("Integration")]
public class PatientStatusApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record PatientDto(Guid Id, string Status, DateTime? DeactivatedAt, string? DeactivationReason);

    public static TheoryData<string> ClinicRoles => [Roles.ClinicOwner, Roles.Staff, Roles.Staff, Roles.Staff];

    private static async Task<Guid> NewPatientAsync(HttpClient client)
    {
        var created = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Status", lastName = "Api", smsOptIn = false, emailOptIn = false
        });
        created.IsSuccessStatusCode.Should().BeTrue(await created.Content.ReadAsStringAsync());
        return (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Theory]
    [MemberData(nameof(ClinicRoles))]
    public async Task EveryClinicRole_CanDeactivateAndReactivate(string role)
    {
        var client = factory.CreateAuthenticatedClient([role]);
        var patientId = await NewPatientAsync(client);

        var deactivated = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/deactivate", new { reason = "Moved away" });
        deactivated.StatusCode.Should().Be(HttpStatusCode.OK, await deactivated.Content.ReadAsStringAsync());
        var body = await deactivated.Content.ReadFromJsonAsync<PatientDto>();
        body!.Status.Should().Be("Inactive");
        body.DeactivationReason.Should().Be("Moved away");
        body.DeactivatedAt.Should().NotBeNull();

        var reactivated = await client.PostAsync($"/api/v1/patients/{patientId}/reactivate", null);
        reactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reactivated.Content.ReadFromJsonAsync<PatientDto>())!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Deactivate_WithAnUpcomingAppointment_Returns409WithTheCount()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await NewPatientAsync(client);
        await AppointmentSeed.BookAsync(factory, patientId, DateTime.UtcNow.AddDays(3));
        await AppointmentSeed.BookAsync(factory, patientId, DateTime.UtcNow.AddDays(-3)); // past: doesn't count

        var response = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/deactivate", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        // ProblemDetails: errors is [{ name, reason }]; metadata arrives as "code.key".
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").EnumerateArray()
            .ToDictionary(e => e.GetProperty("name").GetString()!, e => e.GetProperty("reason").GetString(), StringComparer.OrdinalIgnoreCase); // names arrive camel-cased
        errors.Should().ContainKey("Patient.HasUpcomingAppointments");
        errors.Should().Contain("Patient.HasUpcomingAppointments.count", "1");
    }

    [Fact]
    public async Task Deactivate_AnotherClinicsPatient_Returns404()
    {
        var ours = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await NewPatientAsync(ours);

        var theirs = factory.CreateAuthenticatedClient([Roles.ClinicOwner], "patstatus-other");

        (await theirs.PostAsJsonAsync($"/api/v1/patients/{patientId}/deactivate", new { }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record PageDto(List<PatientDto> Items, int TotalCount);

    // B3 / D11: inactive patients leave daily lists but stay one filter away.
    [Fact]
    public async Task PatientList_DefaultsToActive_AndStatusFiltersStillWork()
    {
        var client = factory.CreateAuthenticatedClient([Roles.Staff]);
        var marker = $"Zlist{Guid.NewGuid():N}"[..14];
        async Task<Guid> Create()
        {
            var r = await client.PostAsJsonAsync("/api/v1/patients", new { firstName = "List", lastName = marker, smsOptIn = false, emailOptIn = false });
            return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
        }
        var active = await Create();
        var inactive = await Create();
        (await client.PostAsJsonAsync($"/api/v1/patients/{inactive}/deactivate", new { })).EnsureSuccessStatusCode();

        async Task<List<Guid>> Ids(string query) =>
            (await client.GetFromJsonAsync<PageDto>($"/api/v1/patients?search={marker}{query}"))!.Items.Select(p => p.Id).ToList();

        (await Ids("")).Should().BeEquivalentTo([active]);
        (await Ids("&status=Inactive")).Should().BeEquivalentTo([inactive]);
        (await Ids("&status=all")).Should().BeEquivalentTo([active, inactive]);

        var search = await client.GetFromJsonAsync<List<IdDto>>($"/api/v1/patients/search?q={marker}");
        search!.Select(p => p.Id).Should().BeEquivalentTo([active], "booking autocomplete leaves inactive patients out");
    }

    [Fact]
    public async Task SuperAdmin_CannotDeactivate()
    {
        var patientId = await NewPatientAsync(factory.CreateAuthenticatedClient([Roles.ClinicOwner]));

        (await factory.CreateAuthenticatedClient([Roles.SuperAdmin]).PostAsJsonAsync($"/api/v1/patients/{patientId}/deactivate", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
