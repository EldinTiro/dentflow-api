using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>DT-12: one rule per action, consistent with what the role can do next.</summary>
[Collection("Integration")]
public class ClinicalRoleRulesTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private async Task<Guid> PatientAsync()
    {
        var r = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).PostAsJsonAsync("/api/v1/patients",
            new { firstName = "Role", lastName = "Rules", smsOptIn = false, emailOptIn = false });
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    // Two roles since 2026-11-02: everyone in the clinic (Staff) works on treatment plans
    [Theory]
    [InlineData(Roles.ClinicOwner, HttpStatusCode.Created)]
    [InlineData(Roles.Staff, HttpStatusCode.Created)]
    public async Task TreatmentPlans_AreCreatedByEveryClinicRole(string role, HttpStatusCode expected)
    {
        var patientId = await PatientAsync();

        var response = await factory.CreateAuthenticatedClient([role])
            .PostAsJsonAsync($"/api/v1/patients/{patientId}/treatment-plans", new { title = "Plan" });

        response.StatusCode.Should().Be(expected);
    }

    // A dentist couldn't cancel, so couldn't deactivate a patient with a visit ahead (D10, R2)
    [Fact]
    public async Task ADentist_CanCancelAnAppointment()
    {
        var owner = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await PatientAsync();
        var staff = await owner.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Cancel", email = $"dr-cancel-{Guid.NewGuid():N}@clinic.test",
        });
        var type = await owner.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"cancel-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981",
        });
        var start = DateTime.UtcNow.Date.AddDays(20).AddHours(10);
        var booked = await owner.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId,
            providerId = (await staff.Content.ReadFromJsonAsync<IdDto>())!.Id,
            appointmentTypeId = (await type.Content.ReadFromJsonAsync<IdDto>())!.Id,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), source = "Staff",
        });
        var appointmentId = (await booked.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await factory.CreateAuthenticatedClient([Roles.Staff])
            .PostAsJsonAsync($"/api/v1/appointments/{appointmentId}/cancel", new { reason = "Patient moved" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
