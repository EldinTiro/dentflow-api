using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>GET /staff/blocked-times: every absence in the clinic that overlaps a range (dashboard "who is away").</summary>
[Collection("Integration")]
public class ClinicAbsencesApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record AbsenceDto(Guid Id, Guid StaffMemberId, DateTime StartAt, DateTime EndAt, string? AbsenceType);

    private async Task<(Guid TenantId, string Slug, HttpClient Owner)> ClinicAsync()
    {
        var slug = $"away-{Guid.NewGuid():N}"[..20];
        var tenantId = await factory.SeedTenantAsync(slug, "Away Clinic");
        return (tenantId, slug, factory.CreateClientForTenant(tenantId, slug, Roles.ClinicOwner));
    }

    private static async Task<Guid> StaffAsync(HttpClient owner, string firstName)
    {
        var r = await owner.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName, lastName = "Test", email = $"{Guid.NewGuid():N}@clinic.com", colorHex = "#3B82F6"
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static async Task AbsenceAsync(HttpClient owner, Guid staffId, string start, string end, string type = "Vacation")
    {
        var r = await owner.PostAsJsonAsync($"/api/v1/staff/{staffId}/blocked-times", new { startAt = start, endAt = end, absenceType = type });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_ReturnsAbsencesOfEveryStaffMemberThatOverlapTheRange()
    {
        var (tenantId, slug, owner) = await ClinicAsync();
        var eldina = await StaffAsync(owner, "Eldina");
        var ana = await StaffAsync(owner, "Ana");
        await AbsenceAsync(owner, eldina, "2026-10-28T07:00:00Z", "2026-10-30T16:00:00Z");           // inside
        await AbsenceAsync(owner, ana, "2026-10-20T07:00:00Z", "2026-10-26T16:00:00Z", "Sickness");  // started before, still running
        await AbsenceAsync(owner, ana, "2026-11-20T07:00:00Z", "2026-11-21T16:00:00Z");              // after the range

        // Every role sees the dashboard, so a receptionist must be able to read it
        var reception = factory.CreateClientForTenant(tenantId, slug, Roles.Staff);
        var r = await reception.GetAsync("/api/v1/staff/blocked-times?from=2026-10-25T23:00:00Z&to=2026-11-08T23:00:00Z");

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var list = (await r.Content.ReadFromJsonAsync<List<AbsenceDto>>())!;
        list.Select(a => (a.StaffMemberId, a.AbsenceType)).Should().Equal((ana, "Sickness"), (eldina, "Vacation"));
    }

    [Fact]
    public async Task List_DoesNotShowAnotherClinicsAbsences()
    {
        var (_, _, owner) = await ClinicAsync();
        var (_, _, otherOwner) = await ClinicAsync();
        await AbsenceAsync(otherOwner, await StaffAsync(otherOwner, "Other"), "2026-10-28T07:00:00Z", "2026-10-29T16:00:00Z");

        var r = await owner.GetAsync("/api/v1/staff/blocked-times?from=2026-10-25T00:00:00Z&to=2026-11-08T00:00:00Z");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<List<AbsenceDto>>()).Should().BeEmpty();
    }

    [Fact]
    public async Task List_WithoutARange_Returns400()
    {
        var (_, _, owner) = await ClinicAsync();

        var r = await owner.GetAsync("/api/v1/staff/blocked-times");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
