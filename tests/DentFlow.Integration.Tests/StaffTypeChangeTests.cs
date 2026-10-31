using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// The staff type (what someone does) can be changed after the profile exists. User report: the
/// clinic owner also treats patients, but her profile was made from her owner account as
/// "Office manager", so she could not be booked.
/// </summary>
[Collection("Integration")]
public class StaffTypeChangeTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record AccessDto(string Role);
    private record StaffDto(Guid Id, string StaffType, bool IsBookable, string? Email, AccessDto? Access);

    private async Task<(Guid TenantId, string Slug, HttpClient Owner)> ClinicAsync()
    {
        var slug = $"type-{Guid.NewGuid():N}"[..20];
        var tenantId = await factory.SeedTenantAsync(slug, "Type Clinic");
        // Roles must exist before an account can be created with one.
        foreach (var role in Roles.TenantRoles)
            await factory.SeedUserAsync($"{role.ToLowerInvariant()}-{slug}@seed.test", "Seed123!@#abcd", role, tenantId);
        return (tenantId, slug, factory.CreateClientForTenant(tenantId, slug, Roles.ClinicOwner));
    }

    private static async Task<StaffDto> CreateAsync(HttpClient owner, string staffType, string? accessRole = null)
    {
        var r = await owner.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType, firstName = "Alma", lastName = "Trako", email = $"alma-{Guid.NewGuid():N}@clinic.com", colorHex = "#3B82F6",
            access = accessRole is null ? null : new { role = accessRole },
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        var id = (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
        return (await owner.GetFromJsonAsync<StaffDto>($"/api/v1/staff/{id}"))!;
    }

    // A linked account locks the email, so the edit sends it back unchanged
    private static Task<HttpResponseMessage> RetypeAsync(HttpClient owner, StaffDto who, string staffType) =>
        owner.PutAsJsonAsync($"/api/v1/staff/{who.Id}", new { firstName = "Alma", lastName = "Trako", email = who.Email, staffType });

    [Fact]
    public async Task Update_AnOwnerToDentist_MakesThemBookable_AndKeepsTheirOwnerAccess()
    {
        var (_, _, owner) = await ClinicAsync();
        var alma = await CreateAsync(owner, "OfficeManager", Roles.ClinicOwner);
        alma.IsBookable.Should().BeFalse();

        var r = await RetypeAsync(owner, alma, "Dentist");

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var updated = (await r.Content.ReadFromJsonAsync<StaffDto>())!;
        updated.StaffType.Should().Be("Dentist");
        updated.IsBookable.Should().BeTrue();
        (await owner.GetFromJsonAsync<StaffDto>($"/api/v1/staff/{alma.Id}"))!.Access!.Role.Should().Be(Roles.ClinicOwner);
    }

    [Fact]
    public async Task Update_WithoutAStaffType_KeepsIt()
    {
        var (_, _, owner) = await ClinicAsync();
        var dentist = await CreateAsync(owner, "Dentist");

        var r = await owner.PutAsJsonAsync($"/api/v1/staff/{dentist.Id}", new { firstName = "Alma", lastName = "Trako", email = dentist.Email });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<StaffDto>())!.StaffType.Should().Be("Dentist");
    }

    // Visits ahead would be left with someone who can no longer be booked
    [Fact]
    public async Task Update_ADentistWithVisitsAheadToNonClinical_Returns409()
    {
        var (_, _, owner) = await ClinicAsync();
        var dentist = await CreateAsync(owner, "Dentist");
        var patient = await owner.PostAsJsonAsync("/api/v1/patients", new { firstName = "P", lastName = "Q", smsOptIn = false, emailOptIn = false });
        var type = await owner.PostAsJsonAsync("/api/v1/appointment-types", new { name = $"t-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981" });
        var start = DateTime.UtcNow.Date.AddDays(7).AddHours(9);
        (await owner.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId = (await patient.Content.ReadFromJsonAsync<IdDto>())!.Id,
            providerId = dentist.Id,
            appointmentTypeId = (await type.Content.ReadFromJsonAsync<IdDto>())!.Id,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), source = "Staff",
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var r = await RetypeAsync(owner, dentist, "Receptionist");

        r.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Staff.HasUpcomingAppointments");
    }

    [Fact]
    public async Task Update_WithAnUnknownStaffType_Returns400()
    {
        var (_, _, owner) = await ClinicAsync();
        var dentist = await CreateAsync(owner, "Dentist");

        (await RetypeAsync(owner, dentist, "Wizard")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
