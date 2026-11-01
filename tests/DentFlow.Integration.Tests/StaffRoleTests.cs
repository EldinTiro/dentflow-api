using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Two clinic roles (user decision 2026-11-02): the sign-in role says what someone may do,
/// the staff type says what they are. Staff ("Osoblje") does the daily work of the clinic;
/// clinic money, people and settings stay with the Administrator (ClinicOwner).
/// </summary>
[Collection("Integration")]
public class StaffRoleTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private async Task<(HttpClient Admin, HttpClient Staff)> ClinicAsync()
    {
        var slug = $"staff{Guid.NewGuid():N}"[..12];
        var tenantId = await factory.SeedTenantAsync(slug, "Staff Role Clinic");
        return (factory.CreateClientForTenant(tenantId, slug, Roles.ClinicOwner),
                factory.CreateClientForTenant(tenantId, slug, Roles.Staff));
    }

    private static async Task<Guid> IdOf(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Fact]
    public async Task Staff_DoesTheDailyWorkOfTheClinic()
    {
        var (admin, staff) = await ClinicAsync();
        var dentistId = await IdOf(await admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Daily", email = $"dr-{Guid.NewGuid():N}@clinic.test",
        }));
        var typeId = await IdOf(await admin.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"daily-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981",
        }));

        var patientId = await IdOf(await staff.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Daily", lastName = "Work", smsOptIn = false, emailOptIn = false,
        }));
        var start = DateTime.UtcNow.Date.AddDays(10).AddHours(10);
        var appointmentId = await IdOf(await staff.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId, providerId = dentistId, appointmentTypeId = typeId,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), source = "Staff",
        }));
        await IdOf(await staff.PostAsJsonAsync($"/api/v1/patients/{patientId}/treatment-plans", new { title = "Plan" }));
        (await staff.PostAsJsonAsync($"/api/v1/patients/{patientId}/teeth", new { tooth = 36, condition = "Filling" }))
            .IsSuccessStatusCode.Should().BeTrue();
        (await staff.GetAsync("/api/v1/tenant/settings")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await staff.GetAsync($"/api/v1/appointments/{appointmentId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var invoiceId = await IdOf(await staff.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices",
            new { lineItems = new[] { new { description = "Check-up", quantity = 1, unitFee = 40m } } }));
        (await staff.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount = 40m, method = "Cash", paidAt = DateTime.UtcNow })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Staff_CannotTouchPeopleSettingsOrTheServiceList()
    {
        var (_, staff) = await ClinicAsync();

        (await staff.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "No", lastName = "Hire", email = $"no-{Guid.NewGuid():N}@clinic.test",
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.PutAsJsonAsync("/api/v1/tenant/settings", new { slotDurationMinutes = 30, weeklyScheduleJson = (string?)null }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = "Nope", defaultDurationMinutes = 30, colorHex = "#10B981",
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TheAdministrator_CanGiveSomeoneTheStaffRole()
    {
        var (admin, _) = await ClinicAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "New", lastName = "Colleague", email = $"new-{Guid.NewGuid():N}@clinic.test",
            access = new { role = Roles.Staff },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Dentist")]
    [InlineData("DentalNurse")]
    [InlineData("Receptionist")]
    public async Task TheOldTitleRoles_CanNoLongerBeGiven(string oldRole)
    {
        var (admin, _) = await ClinicAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Old", lastName = "Title", email = $"old-{Guid.NewGuid():N}@clinic.test",
            access = new { role = oldRole },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
