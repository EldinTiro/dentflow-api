using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>
/// People-merge plan 3.1: after the backfill every clinic account has exactly one staff
/// profile — linked when an unambiguous profile with its email exists, created otherwise.
/// </summary>
[Collection("Integration")]
public class StaffAccountBackfillTests(DentFlowAppFactory factory)
{
    private const string Password = "Back123!@#abcde";

    private async Task<Guid> ClinicAsync() =>
        await factory.SeedTenantAsync($"back-{Guid.NewGuid():N}"[..18], "Backfill Clinic");

    private async Task<Guid> ProfileAsync(Guid tenantId, string? email, StaffType type = StaffType.Dentist, bool active = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var staff = StaffMember.Create(type, "Profile", "Person", email, null, null);
        staff.SetTenant(tenantId);
        if (!active) staff.Deactivate();
        db.Set<StaffMember>().Add(staff);
        await db.SaveChangesAsync();
        return staff.Id;
    }

    private async Task<List<StaffMember>> ProfilesOfAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Set<StaffMember>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted).ToListAsync();
    }

    private Task<StaffBackfillSummary> RunAsync() => StaffAccountBackfill.RunAsync(factory.Services);

    [Fact]
    public async Task AccountWithAMatchingProfile_IsLinked_CaseInsensitively()
    {
        var tenantId = await ClinicAsync();
        var email = $"match-{Guid.NewGuid():N}@back.test";
        var profileId = await ProfileAsync(tenantId, email.ToUpperInvariant());
        var userId = await factory.SeedUserAsync(email, Password, Roles.Dentist, tenantId);

        await RunAsync();

        (await ProfilesOfAsync(userId)).Should().ContainSingle().Which.Id.Should().Be(profileId);
    }

    [Theory]
    [InlineData(Roles.Dentist, StaffType.Dentist)]
    [InlineData(Roles.DentalNurse, StaffType.DentalAssistant)]
    [InlineData(Roles.Receptionist, StaffType.Receptionist)]
    [InlineData(Roles.ClinicOwner, StaffType.OfficeManager)]
    public async Task AccountWithoutAProfile_GetsOneCreated_FromItsNameAndRole(string role, StaffType expected)
    {
        var tenantId = await ClinicAsync();
        var email = $"new-{Guid.NewGuid():N}@back.test";
        var userId = await factory.SeedUserAsync(email, Password, role, tenantId);

        await RunAsync();

        var profile = (await ProfilesOfAsync(userId)).Should().ContainSingle().Subject;
        profile.StaffType.Should().Be(expected);
        profile.Email.Should().Be(email);
        profile.FullName.Should().Be("Test User");
        profile.TenantId.Should().Be(tenantId);
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task TwoCandidateProfiles_AreSkipped_AndNothingChanges()
    {
        var tenantId = await ClinicAsync();
        var email = $"dup-{Guid.NewGuid():N}@back.test";
        await ProfileAsync(tenantId, email);
        // The unique email index only covers non-deleted rows with the exact same text.
        await ProfileAsync(tenantId, email.ToUpperInvariant());
        var userId = await factory.SeedUserAsync(email, Password, Roles.Dentist, tenantId);

        var summary = await RunAsync();

        (await ProfilesOfAsync(userId)).Should().BeEmpty();
        summary.Skipped.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SuperAdmin_IsIgnored()
    {
        var userId = await factory.SeedUserAsync($"sa-{Guid.NewGuid():N}@back.test", Password, Roles.SuperAdmin, null);

        await RunAsync();

        (await ProfilesOfAsync(userId)).Should().BeEmpty();
    }

    [Fact]
    public async Task RunningItTwice_ChangesNothingTheSecondTime()
    {
        var tenantId = await ClinicAsync();
        await factory.SeedUserAsync($"twice-{Guid.NewGuid():N}@back.test", Password, Roles.Receptionist, tenantId);
        await RunAsync();

        var second = await RunAsync();

        second.Linked.Should().Be(0);
        second.Created.Should().Be(0);
    }

    [Fact]
    public async Task DeactivatedAccount_GetsAnInactiveProfile()
    {
        var tenantId = await ClinicAsync();
        var email = $"off-{Guid.NewGuid():N}@back.test";
        var userId = await factory.SeedUserAsync(email, Password, Roles.Dentist, tenantId);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(userId.ToString()))!;
            user.IsActive = false;
            await users.UpdateAsync(user);
        }

        await RunAsync();

        var profile = (await ProfilesOfAsync(userId)).Should().ContainSingle().Subject;
        profile.IsActive.Should().BeFalse();
        profile.IsBookable.Should().BeFalse();
    }

    [Fact]
    public async Task AnInactiveMatchingProfile_IsStillLinked()
    {
        var tenantId = await ClinicAsync();
        var email = $"old-{Guid.NewGuid():N}@back.test";
        var profileId = await ProfileAsync(tenantId, email, active: false);
        var userId = await factory.SeedUserAsync(email, Password, Roles.Dentist, tenantId);

        await RunAsync();

        (await ProfilesOfAsync(userId)).Should().ContainSingle().Which.Id.Should().Be(profileId);
    }
}
