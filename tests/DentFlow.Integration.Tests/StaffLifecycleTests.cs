using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>People-merge plan 2.6 (D4): a person's account follows their staff profile.</summary>
[Collection("Integration")]
public class StaffLifecycleTests(DentFlowAppFactory factory)
{
    private const string Password = "Life123!@#abcd";
    private record LoginDto(string AccessToken, Guid UserId);

    private async Task<(string Slug, Guid TenantId)> ClinicAsync()
    {
        var slug = $"life-{Guid.NewGuid():N}"[..18];
        return (slug, await factory.SeedTenantAsync(slug, "Life Clinic"));
    }

    /// <summary>An account plus a staff profile linked to it, as the backfill/grant would leave them.</summary>
    private async Task<(Guid UserId, Guid StaffId, string Email)> PersonAsync(Guid tenantId, string role, StaffType type)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@life.test";
        var userId = await factory.SeedUserAsync(email, Password, role, tenantId);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var staff = StaffMember.Create(type, "Test", role, email, null, null);
        staff.SetTenant(tenantId);
        staff.LinkUser(userId);
        db.Set<StaffMember>().Add(staff);
        await db.SaveChangesAsync();
        return (userId, staff.Id, email);
    }

    private async Task<HttpResponseMessage> LoginAsync(string slug, string email) =>
        await factory.CreateAnonymousClient(slug).PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

    private async Task<HttpClient> SignedInAsync(string slug, string email)
    {
        var login = await LoginAsync(slug, email);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var client = factory.CreateAnonymousClient(slug);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken);
        return client;
    }

    [Fact]
    public async Task DeletingALinkedProfile_EndsThatPersonsSignIn()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, dentistStaffId, dentistEmail) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var owner = await SignedInAsync(slug, ownerEmail);

        (await owner.DeleteAsync($"/api/v1/staff/{dentistStaffId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await LoginAsync(slug, dentistEmail)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletingYourOwnLinkedProfile_Returns409_AndChangesNothing()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, ownerStaffId, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var owner = await SignedInAsync(slug, ownerEmail);

        var response = await owner.DeleteAsync($"/api/v1/staff/{ownerStaffId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("user.self");
        (await owner.GetAsync($"/api/v1/staff/{ownerStaffId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoginAsync(slug, ownerEmail)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 2.7 (D6): one name for profile and account ──────────────────────

    private record NameDto(string FirstName, string LastName, string FullName);

    [Fact]
    public async Task UpdateOwnName_AlsoRenamesLinkedStaffProfile()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, staffId, email) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var dentist = await SignedInAsync(slug, email);

        (await dentist.PutAsJsonAsync("/api/v1/users/me", new { firstName = "Amra", lastName = "Hadžić" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await dentist.GetFromJsonAsync<NameDto>($"/api/v1/staff/{staffId}"))!.FullName.Should().Be("Amra Hadžić");
    }

    [Fact]
    public async Task EditingALinkedProfilesName_RenamesTheAccount()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, staffId, email) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var owner = await SignedInAsync(slug, ownerEmail);

        (await owner.PutAsJsonAsync($"/api/v1/staff/{staffId}", new { firstName = "Lejla", lastName = "Kovač", email }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var dentist = await SignedInAsync(slug, email);
        (await dentist.GetFromJsonAsync<NameDto>("/api/v1/users/me"))!.FullName.Should().Be("Lejla Kovač");
    }

    [Fact]
    public async Task ChangingALinkedProfilesEmail_Returns400()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, staffId, _) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var owner = await SignedInAsync(slug, ownerEmail);

        var response = await owner.PutAsJsonAsync($"/api/v1/staff/{staffId}", new { firstName = "Test", lastName = "Dentist", email = "other@life.test" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("staff.EmailLockedByAccess");
    }
    // ── B6 (R1, D10–D12): deactivate instead of delete ──────────────────

    private record StaffDto(Guid Id, bool IsActive, bool IsBookable);
    private record StaffPageDto(List<StaffDto> Items);

    [Fact]
    public async Task DeactivatingALinkedDentist_EndsSignIn_AndReactivatingDoesNotRestoreIt()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, staffId, dentistEmail) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var owner = await SignedInAsync(slug, ownerEmail);

        var deactivated = await owner.PostAsync($"/api/v1/staff/{staffId}/deactivate", null);
        deactivated.StatusCode.Should().Be(HttpStatusCode.OK, await deactivated.Content.ReadAsStringAsync());
        var body = await deactivated.Content.ReadFromJsonAsync<StaffDto>();
        body!.IsActive.Should().BeFalse();
        body.IsBookable.Should().BeFalse();
        (await LoginAsync(slug, dentistEmail)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var reactivated = await owner.PostAsync($"/api/v1/staff/{staffId}/reactivate", null);
        reactivated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reactivated.Content.ReadFromJsonAsync<StaffDto>())!.IsActive.Should().BeTrue();
        (await LoginAsync(slug, dentistEmail)).StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the owner gives access back explicitly (D4)");
    }

    [Fact]
    public async Task DeactivatingADentistWithAnUpcomingAppointment_Returns409WithTheCount()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, staffId, _) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        await AppointmentSeed.BookWithProviderAsync(factory, tenantId, staffId, DateTime.UtcNow.AddDays(2));
        var owner = await SignedInAsync(slug, ownerEmail);

        var response = await owner.PostAsync($"/api/v1/staff/{staffId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errors = (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errors").EnumerateArray()
            .ToDictionary(e => e.GetProperty("name").GetString()!, e => e.GetProperty("reason").GetString(), StringComparer.OrdinalIgnoreCase);
        errors.Should().Contain("Staff.HasUpcomingAppointments.count", "1");
    }

    [Fact]
    public async Task DeactivatingYourOwnProfile_Returns409()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, ownerStaffId, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var owner = await SignedInAsync(slug, ownerEmail);

        var response = await owner.PostAsync($"/api/v1/staff/{ownerStaffId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("user.self");
    }

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    public async Task OnlyTheOwnerDeactivatesStaff(string role)
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, staffId, _) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var (_, _, callerEmail) = await PersonAsync(tenantId, role, StaffType.Receptionist);
        var caller = await SignedInAsync(slug, callerEmail);

        (await caller.PostAsync($"/api/v1/staff/{staffId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await caller.PostAsync($"/api/v1/staff/{staffId}/reactivate", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StaffList_DefaultsToActive_AndIsActiveAllShowsEveryone()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, goneId, _) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        var owner = await SignedInAsync(slug, ownerEmail);
        (await owner.PostAsync($"/api/v1/staff/{goneId}/deactivate", null)).EnsureSuccessStatusCode();

        (await owner.GetFromJsonAsync<StaffPageDto>("/api/v1/staff"))!.Items.Should().NotContain(s => s.Id == goneId);
        (await owner.GetFromJsonAsync<StaffPageDto>("/api/v1/staff?isActive=false"))!.Items.Should().Contain(s => s.Id == goneId);
        (await owner.GetFromJsonAsync<StaffPageDto>("/api/v1/staff?isActive=all"))!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeletingAProfileWithAppointments_Returns409HasHistory()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, _, ownerEmail) = await PersonAsync(tenantId, Roles.ClinicOwner, StaffType.OfficeManager);
        var (_, staffId, _) = await PersonAsync(tenantId, Roles.Dentist, StaffType.Dentist);
        await AppointmentSeed.BookWithProviderAsync(factory, tenantId, staffId, DateTime.UtcNow.AddDays(-10));
        var owner = await SignedInAsync(slug, ownerEmail);

        var response = await owner.DeleteAsync($"/api/v1/staff/{staffId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("staff.HasHistory");
    }
}
