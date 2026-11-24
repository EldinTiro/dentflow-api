using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// The Administrator can delete a patient for good, with everything on their record, and a
/// staff profile that has no history (user request 2026-11-13). Everyone else deactivates.
/// </summary>
[Collection("Integration")]
public class PermanentDeleteTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record PageDto(List<IdDto> Items, int TotalCount);

    private async Task<(HttpClient Admin, HttpClient Staff)> ClinicAsync()
    {
        var slug = $"purge{Guid.NewGuid():N}"[..12];
        var tenantId = await factory.SeedTenantAsync(slug, "Purge Clinic");
        return (factory.CreateClientForTenant(tenantId, slug, Roles.ClinicOwner),
                factory.CreateClientForTenant(tenantId, slug, Roles.Staff));
    }

    private static async Task<Guid> IdOf(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static Task<Guid> DentistAsync(HttpClient admin) =>
        IdOf(admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Purge", email = $"dr-{Guid.NewGuid():N}@clinic.test",
        }).Result);

    /// <summary>A patient with a bit of everything: a visit, its invoice and payment, a plan, a tooth, an allergy.</summary>
    private static async Task<(Guid Patient, Guid Appointment, Guid Invoice)> FullRecordAsync(HttpClient admin, Guid dentist)
    {
        var type = await IdOf(await admin.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"purge-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981",
        }));
        var patient = await IdOf(await admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Gone", lastName = "ForGood", smsOptIn = false, emailOptIn = false,
        }));
        var start = DateTime.UtcNow.AddHours(2);
        var appointment = await IdOf(await admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId = patient, providerId = dentist, appointmentTypeId = type,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), source = "Staff",
        }));
        (await admin.PatchAsJsonAsync($"/api/v1/appointments/{appointment}/status", new { status = "CheckedIn" }))
            .IsSuccessStatusCode.Should().BeTrue();
        var invoice = await IdOf(await admin.PostAsJsonAsync($"/api/v1/patients/{patient}/invoices", new
        {
            appointmentId = appointment,
            lineItems = new[] { new { description = "Extraction", quantity = 1, unitFee = 50m } },
        }));
        (await admin.PostAsJsonAsync($"/api/v1/invoices/{invoice}/payments", new
        {
            amount = 20m, method = "Cash", paidAt = DateTime.UtcNow.ToString("o"),
        })).IsSuccessStatusCode.Should().BeTrue();
        await IdOf(await admin.PostAsJsonAsync($"/api/v1/patients/{patient}/treatment-plans", new { title = "Plan" }));
        (await admin.PostAsJsonAsync($"/api/v1/patients/{patient}/teeth", new { tooth = 36, condition = "Filling" }))
            .IsSuccessStatusCode.Should().BeTrue();
        (await admin.PostAsJsonAsync($"/api/v1/patients/{patient}/allergies", new { allergen = "Penicillin", severity = "Severe" }))
            .IsSuccessStatusCode.Should().BeTrue();
        return (patient, appointment, invoice);
    }

    [Fact]
    public async Task Patient_IsDeletedForGood_WithEverythingOnTheRecord()
    {
        var (admin, _) = await ClinicAsync();
        var dentist = await DentistAsync(admin);
        var (patient, appointment, invoice) = await FullRecordAsync(admin, dentist);

        var r = await admin.DeleteAsync($"/api/v1/patients/{patient}/permanent");

        r.StatusCode.Should().Be(HttpStatusCode.NoContent, await r.Content.ReadAsStringAsync());
        (await admin.GetAsync($"/api/v1/patients/{patient}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync($"/api/v1/appointments/{appointment}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync($"/api/v1/invoices/{invoice}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        // The doctor had a visit only with this patient: now they have none, and can go too
        (await admin.DeleteAsync($"/api/v1/staff/{dentist}/permanent")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Patient_PermanentDelete_IsForTheAdministratorOnly()
    {
        var (admin, staff) = await ClinicAsync();
        var patient = await IdOf(await admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Stays", lastName = "Here", smsOptIn = false, emailOptIn = false,
        }));

        (await staff.DeleteAsync($"/api/v1/patients/{patient}/permanent")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.GetAsync($"/api/v1/patients/{patient}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Patient_OfAnotherClinic_IsNotFound()
    {
        var (admin, _) = await ClinicAsync();
        var (otherAdmin, _) = await ClinicAsync();
        var theirs = await IdOf(await otherAdmin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Not", lastName = "Yours", smsOptIn = false, emailOptIn = false,
        }));

        (await admin.DeleteAsync($"/api/v1/patients/{theirs}/permanent")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherAdmin.GetAsync($"/api/v1/patients/{theirs}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Staff_WithoutHistory_IsDeletedForGood()
    {
        var (admin, _) = await ClinicAsync();
        var dentist = await DentistAsync(admin);

        var r = await admin.DeleteAsync($"/api/v1/staff/{dentist}/permanent");

        r.StatusCode.Should().Be(HttpStatusCode.NoContent, await r.Content.ReadAsStringAsync());
        (await admin.GetAsync($"/api/v1/staff/{dentist}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var everyone = (await admin.GetFromJsonAsync<PageDto>("/api/v1/staff?isActive=all&pageSize=100"))!;
        everyone.Items.Should().NotContain(s => s.Id == dentist);
    }

    // User decision 2026-11-13: a doctor's visits belong to the patients' records too
    [Fact]
    public async Task Staff_WithAppointments_IsKept_Returns409()
    {
        var (admin, _) = await ClinicAsync();
        var dentist = await DentistAsync(admin);
        await FullRecordAsync(admin, dentist);

        var r = await admin.DeleteAsync($"/api/v1/staff/{dentist}/permanent");

        r.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Staff.HasHistory");
    }

    [Fact]
    public async Task Staff_PermanentDelete_IsForTheAdministratorOnly()
    {
        var (admin, staff) = await ClinicAsync();
        var dentist = await DentistAsync(admin);

        (await staff.DeleteAsync($"/api/v1/staff/{dentist}/permanent")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
