using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-07: with Team gone, Staff is the only list of people. Accounts made by SuperAdmin
/// provisioning, the old /users/invite and tenant creation had no staff profile, so the
/// owner could not see them until the next restart ran the backfill.
/// </summary>
[Collection("Integration")]
public class AccountsGetAStaffProfileTests(DentFlowAppFactory factory)
{
    private record UserIdDto(Guid UserId);

    private async Task<StaffMember?> ProfileOfAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<StaffMember>().IgnoreQueryFilters().SingleOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
    }

    private async Task<ApplicationUser> UserAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!;
    }

    [Fact]
    public async Task SuperAdminProvisioning_CreatesTheLinkedProfile_InThatClinic()
    {
        var slug = $"prov-{Guid.NewGuid():N}"[..16];
        var tenantId = await factory.SeedTenantAsync(slug, "Prov Clinic");
        var email = $"prov-{Guid.NewGuid():N}@prov.test";

        var response = await factory.CreatePlatformAdminClient().PostAsJsonAsync(
            $"/api/v1/admin/tenants/{tenantId}/users",
            new { email, firstName = "Prov", lastName = "Nurse", role = Roles.DentalNurse });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var userId = (await response.Content.ReadFromJsonAsync<UserIdDto>())!.UserId;

        var profile = await ProfileOfAsync(userId);
        profile.Should().NotBeNull();
        profile!.TenantId.Should().Be(tenantId);
        profile.Email.Should().Be(email);
        profile.FullName.Should().Be("Prov Nurse");
        profile.StaffType.Should().Be(StaffType.DentalAssistant);
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task OwnerInvite_CreatesTheLinkedProfile()
    {
        var email = $"inv-{Guid.NewGuid():N}@inv.test";

        var response = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).PostAsJsonAsync(
            "/api/v1/users/invite", new { email, firstName = "Inv", lastName = "Dentist", role = Roles.Dentist });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        var userId = (await response.Content.ReadFromJsonAsync<UserIdDto>())!.UserId;

        (await ProfileOfAsync(userId))!.StaffType.Should().Be(StaffType.Dentist);
    }

    [Fact]
    public async Task CreatingATenant_GivesTheOwnerAProfile()
    {
        var slug = $"new-{Guid.NewGuid():N}"[..14];
        var email = $"owner-{Guid.NewGuid():N}@new.test";

        var response = await factory.CreatePlatformAdminClient().PostAsJsonAsync("/api/v1/tenants", new
        {
            slug, name = "New Clinic", plan = "Free", ownerEmail = email, ownerFirstName = "New", ownerLastName = "Owner",
        });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        var owner = await UserAsync(email);
        var profile = await ProfileOfAsync(owner.Id);
        profile.Should().NotBeNull();
        profile!.TenantId.Should().Be(owner.TenantId!.Value);
        profile.StaffType.Should().Be(StaffType.OfficeManager);
    }
}
