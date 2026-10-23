using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>People-merge plan 2.2 / 2.3: staff profiles with an optional sign-in account.</summary>
[Collection("Integration")]
public class StaffAccessApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record GrantDto(Guid UserId, string TempPassword);
    private record LoginDto(string AccessToken, bool MustChangePassword);

    private async Task<(string Slug, Guid TenantId, HttpClient Owner)> ClinicAsync()
    {
        var slug = $"access-{Guid.NewGuid():N}"[..20];
        var tenantId = await factory.SeedTenantAsync(slug, "Access Clinic");
        // Roles must exist before an account can be created with one.
        foreach (var role in Roles.TenantRoles)
            await factory.SeedUserAsync($"{role.ToLowerInvariant()}-{slug}@seed.test", "Seed123!@#abcd", role, tenantId);
        return (slug, tenantId, factory.CreateClientForTenant(tenantId, slug, Roles.ClinicOwner));
    }

    private static async Task<(Guid Id, string Email)> ProfileAsync(HttpClient owner)
    {
        var email = $"dr-{Guid.NewGuid():N}@clinic.com";
        var r = await owner.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Amra", lastName = "Begić", email, colorHex = "#3B82F6"
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return ((await r.Content.ReadFromJsonAsync<IdDto>())!.Id, email);
    }

    [Fact]
    public async Task GrantAccess_TheNewAccountCanSignIn_AndMustChangeItsPassword()
    {
        var (slug, _, owner) = await ClinicAsync();
        var (staffId, email) = await ProfileAsync(owner);

        var grant = await owner.PostAsJsonAsync($"/api/v1/staff/{staffId}/access", new { role = Roles.Dentist });

        grant.StatusCode.Should().Be(HttpStatusCode.OK, await grant.Content.ReadAsStringAsync());
        var dto = (await grant.Content.ReadFromJsonAsync<GrantDto>())!;
        var login = await factory.CreateAnonymousClient(slug).PostAsJsonAsync("/api/v1/auth/login", new { email, password = dto.TempPassword });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await login.Content.ReadFromJsonAsync<LoginDto>())!.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task GrantAccess_Twice_Returns409()
    {
        var (_, _, owner) = await ClinicAsync();
        var (staffId, _) = await ProfileAsync(owner);
        await owner.PostAsJsonAsync($"/api/v1/staff/{staffId}/access", new { role = Roles.Dentist });

        var again = await owner.PostAsJsonAsync($"/api/v1/staff/{staffId}/access", new { role = Roles.Dentist });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GrantAccess_ToAnotherClinicsProfile_Returns404()
    {
        var (_, _, ownerA) = await ClinicAsync();
        var (_, _, ownerB) = await ClinicAsync();
        var (staffId, _) = await ProfileAsync(ownerA);

        var response = await ownerB.PostAsJsonAsync($"/api/v1/staff/{staffId}/access", new { role = Roles.Dentist });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GrantAccess_AsReceptionist_Returns403()
    {
        var (slug, tenantId, owner) = await ClinicAsync();
        var (staffId, _) = await ProfileAsync(owner);
        var receptionist = factory.CreateClientForTenant(tenantId, slug, Roles.Receptionist);

        var response = await receptionist.PostAsJsonAsync($"/api/v1/staff/{staffId}/access", new { role = Roles.Dentist });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 2.3: profile + account in one request ──────────────────────────

    private record CreatedDto(Guid Id, string? TempPassword);
    private record StaffRow(Guid Id, string? Email);
    private record Paged(List<StaffRow> Items);

    [Fact]
    public async Task CreateWithAccess_CreatesBoth_InOneRequest()
    {
        var (slug, _, owner) = await ClinicAsync();
        var email = $"dr-{Guid.NewGuid():N}@clinic.com";

        var created = await owner.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Amra", lastName = "Begić", email, colorHex = "#3B82F6",
            access = new { role = Roles.Dentist }
        });

        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var dto = (await created.Content.ReadFromJsonAsync<CreatedDto>())!;
        dto.TempPassword.Should().NotBeNullOrWhiteSpace();
        var login = await factory.CreateAnonymousClient(slug).PostAsJsonAsync("/api/v1/auth/login", new { email, password = dto.TempPassword });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        (await owner.GetStringAsync($"/api/v1/staff/{dto.Id}")).Should().NotContain("tempPassword", "the password is shown once, on creation");
    }

    [Fact]
    public async Task CreateWithAccess_WhenTheEmailIsTakenOnThePlatform_Returns409_AndLeavesNoProfile()
    {
        var (_, tenantId, owner) = await ClinicAsync();
        var email = $"taken-{Guid.NewGuid():N}@clinic.com";
        await factory.SeedUserAsync(email, "Taken123!@#abcd", Roles.Dentist, tenantId);

        var created = await owner.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Amra", lastName = "Begić", email, colorHex = "#3B82F6",
            access = new { role = Roles.Dentist }
        });

        created.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var list = await owner.GetFromJsonAsync<Paged>("/api/v1/staff?pageSize=100");
        list!.Items.Should().NotContain(s => s.Email == email, "the profile insert is rolled back with the failed account");
    }

    // ── 2.4: access summary on staff responses ────────────────────────

    private record AccessDto(Guid UserId, string? Role, bool IsActive, bool MustChangePassword, DateTime? LastLoginAt);
    private record StaffWithAccess(Guid Id, string? Email, AccessDto? Access);
    private record PagedWithAccess(List<StaffWithAccess> Items);

    [Fact]
    public async Task List_ShowsAccessForLinkedProfiles_AndNullForOthers()
    {
        var (_, _, owner) = await ClinicAsync();
        var (linkedId, _) = await ProfileAsync(owner);
        var (plainId, _) = await ProfileAsync(owner);
        await owner.PostAsJsonAsync($"/api/v1/staff/{linkedId}/access", new { role = Roles.DentalNurse });

        var list = await owner.GetFromJsonAsync<PagedWithAccess>("/api/v1/staff?pageSize=100");

        var linked = list!.Items.Single(s => s.Id == linkedId).Access;
        linked.Should().NotBeNull();
        linked!.Role.Should().Be(Roles.DentalNurse);
        linked.IsActive.Should().BeTrue();
        linked.MustChangePassword.Should().BeTrue();
        linked.LastLoginAt.Should().BeNull();
        list.Items.Single(s => s.Id == plainId).Access.Should().BeNull();
        (await owner.GetFromJsonAsync<StaffWithAccess>($"/api/v1/staff/{linkedId}"))!.Access!.Role.Should().Be(Roles.DentalNurse);
    }
}
