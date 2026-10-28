using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>People-merge plan B4 / D9: delete is for mistakes, deactivate is for everything else.</summary>
[Collection("Integration")]
public class PatientDeleteRulesTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    public static TheoryData<string> ClinicRoles => [Roles.ClinicOwner, Roles.Dentist, Roles.DentalNurse, Roles.Receptionist];

    private static async Task<Guid> NewPatientAsync(HttpClient client)
    {
        var created = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Dupli", lastName = "Kat", smsOptIn = false, emailOptIn = false
        });
        created.IsSuccessStatusCode.Should().BeTrue(await created.Content.ReadAsStringAsync());
        return (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Theory]
    [MemberData(nameof(ClinicRoles))]
    public async Task EveryClinicRole_CanDeleteAFreshDuplicate(string role)
    {
        var client = factory.CreateAuthenticatedClient([role]);
        var patientId = await NewPatientAsync(client);

        (await client.DeleteAsync($"/api/v1/patients/{patientId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/v1/patients/{patientId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_PatientWithAPastAppointment_Returns409_AndKeepsThePatient()
    {
        var client = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await NewPatientAsync(client);
        await AppointmentSeed.BookAsync(factory, patientId, DateTime.UtcNow.AddDays(-30));

        var response = await client.DeleteAsync($"/api/v1/patients/{patientId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Patient.HasHistory");
        (await client.GetAsync($"/api/v1/patients/{patientId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // DT-19: "Delete duplicate" was offered on patients with history; the page needs to know
    private record HistoryDto(bool HasHistory);

    [Fact]
    public async Task ThePatient_SaysWhetherItHasHistory()
    {
        var client = factory.CreateAuthenticatedClient([Roles.Receptionist]);
        var fresh = await NewPatientAsync(client);
        var withVisit = await NewPatientAsync(client);
        await AppointmentSeed.BookAsync(factory, withVisit, DateTime.UtcNow.AddDays(-5));

        (await client.GetFromJsonAsync<HistoryDto>($"/api/v1/patients/{fresh}"))!.HasHistory.Should().BeFalse();
        (await client.GetFromJsonAsync<HistoryDto>($"/api/v1/patients/{withVisit}"))!.HasHistory.Should().BeTrue();
    }
}
