using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// People-merge plan B0 / R2: every clinic role can add, edit and deactivate patients,
/// including their medical data. Before this, a Dentist got 403 on emergency contacts.
/// </summary>
[Collection("Integration")]
public class PatientAccessRulesTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    public static TheoryData<string> ClinicRoles => [Roles.ClinicOwner, Roles.Dentist, Roles.DentalNurse, Roles.Receptionist];

    private static async Task<Guid> CreatedId(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Theory]
    [MemberData(nameof(ClinicRoles))]
    public async Task EveryClinicRole_CanManageAPatientAndTheirMedicalData(string role)
    {
        var client = factory.CreateAuthenticatedClient([role]);

        var patientId = await CreatedId(await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Access", lastName = role, smsOptIn = false, emailOptIn = false
        }));

        (await client.PutAsJsonAsync($"/api/v1/patients/{patientId}", new
        {
            firstName = "Access", lastName = role + "Edited", smsOptIn = false, emailOptIn = false
        })).StatusCode.Should().Be(HttpStatusCode.OK, "update");

        var allergyId = await CreatedId(await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/allergies",
            new { allergen = "Penicillin", severity = "Severe" }));
        (await client.DeleteAsync($"/api/v1/patients/{patientId}/allergies/{allergyId}"))
            .IsSuccessStatusCode.Should().BeTrue("delete allergy");

        var contactId = await CreatedId(await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/emergency-contacts",
            new { name = "Mother", relationship = "Mother", phonePrimary = "+38761111222", isPrimary = true }));
        (await client.DeleteAsync($"/api/v1/patients/{patientId}/emergency-contacts/{contactId}"))
            .IsSuccessStatusCode.Should().BeTrue("delete emergency contact");

        (await client.PutAsJsonAsync($"/api/v1/patients/{patientId}/medical-history", new { isSmoker = true }))
            .IsSuccessStatusCode.Should().BeTrue("medical history");
    }

    [Fact]
    public async Task SuperAdmin_CannotCreateClinicPatients()
    {
        var response = await factory.CreateAuthenticatedClient([Roles.SuperAdmin]).PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Platform", lastName = "Admin", smsOptIn = false, emailOptIn = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
