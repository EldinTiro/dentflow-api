using System.Net;
using System.Net.Http.Json;
using DentFlow.Domain.Identity;
using DentFlow.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Simplified roles: Clinic owner, Dentist, Dental nurse, Receptionist. Users in a removed
/// role are moved at startup, and the dental nurse can do chairside and front-desk work
/// but not see clinic finances.
/// </summary>
[Collection("Integration")]
public class RoleSimplificationTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private async Task<ApplicationUser> UserInRetiredRoleAsync(string retiredRole)
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!await roles.RoleExistsAsync(retiredRole))
            (await roles.CreateAsync(new ApplicationRole { Name = retiredRole })).Succeeded.Should().BeTrue();

        var email = $"{retiredRole.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Old", LastName = retiredRole, IsActive = true };
        (await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded.Should().BeTrue();
        (await users.AddToRoleAsync(user, retiredRole)).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<(IList<string> Roles, bool IsActive, bool RoleStillExists)> AfterSyncAsync(Guid userId, string retiredRole)
    {
        await SuperAdminSeeder.SyncRolesAsync(factory.Services);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var user = (await users.FindByIdAsync(userId.ToString()))!;
        return (await users.GetRolesAsync(user), user.IsActive, await roles.RoleExistsAsync(retiredRole));
    }

    [Theory]
    [InlineData("ClinicAdmin", Roles.ClinicOwner)]
    [InlineData("Hygienist", Roles.DentalNurse)]
    [InlineData("BillingStaff", Roles.Receptionist)]
    public async Task SyncRoles_MovesUsersOutOfARemovedRole(string retired, string replacement)
    {
        var user = await UserInRetiredRoleAsync(retired);

        var (userRoles, isActive, roleStillExists) = await AfterSyncAsync(user.Id, retired);

        userRoles.Should().Equal(replacement);
        isActive.Should().BeTrue();
        roleStillExists.Should().BeFalse();
    }

    [Fact]
    public async Task SyncRoles_DeactivatesReadOnlyUsersRatherThanGrantingWriteAccess()
    {
        var user = await UserInRetiredRoleAsync("ReadOnly");

        var (userRoles, isActive, _) = await AfterSyncAsync(user.Id, "ReadOnly");

        userRoles.Should().BeEmpty();
        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task DentalNurse_CanDoChairsideAndFrontDeskWork()
    {
        var slug = $"nurse{Guid.NewGuid():N}"[..12];
        var tenantId = await factory.SeedTenantAsync(slug, "Nurse Clinic");
        var owner = factory.CreateClientForTenant(tenantId, slug, Roles.ClinicOwner);
        var nurse = factory.CreateClientForTenant(tenantId, slug, Roles.DentalNurse);

        var patient = await nurse.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Nurse", lastName = "Booked", email = $"{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        patient.StatusCode.Should().Be(HttpStatusCode.Created);
        var patientId = (await patient.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var invoice = await owner.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices",
            new { lineItems = new[] { new { description = "Cleaning", quantity = 1, unitFee = 60m } } });
        var invoiceId = (await invoice.Content.ReadFromJsonAsync<IdDto>())!.Id;

        (await nurse.GetAsync("/api/v1/staff")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await nurse.GetAsync($"/api/v1/patients/{patientId}/invoices")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await nurse.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount = 60m, method = "Cash", paidAt = DateTime.UtcNow })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/v1/dashboard/finance")]
    [InlineData("/api/v1/invoices")]
    [InlineData("/api/v1/finance/debtors")]
    public async Task DentalNurse_CannotSeeClinicFinances(string url)
    {
        var slug = $"nurse{Guid.NewGuid():N}"[..12];
        var tenantId = await factory.SeedTenantAsync(slug, "Nurse Clinic");
        var nurse = factory.CreateClientForTenant(tenantId, slug, Roles.DentalNurse);

        (await nurse.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
