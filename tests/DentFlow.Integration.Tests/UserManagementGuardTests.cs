using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-012: users could lock themselves (or the platform) out, and a ClinicAdmin could
/// demote, deactivate or create a ClinicOwner. Each test gets its own clinic.
/// </summary>
[Collection("Integration")]
public class UserManagementGuardTests(DentFlowAppFactory factory)
{
    private const string Password = "Guard123!@#abc";

    private record LoginDto(string AccessToken, Guid UserId);

    private sealed record Clinic(string Slug, Guid TenantId);

    private async Task<Clinic> ClinicAsync()
    {
        var slug = $"guard-{Guid.NewGuid():N}"[..20];
        return new Clinic(slug, await factory.SeedTenantAsync(slug, "Guard Clinic"));
    }

    private async Task<Guid> UserAsync(Clinic? clinic, string role) =>
        await factory.SeedUserAsync($"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@guard.test", Password, role, clinic?.TenantId);

    /// <summary>Signs the user in through the real login endpoint, so "sub" is their real id.</summary>
    private async Task<HttpClient> SignedInAsync(Guid userId, string slug)
    {
        string email;
        using (var scope = factory.Services.CreateScope())
            email = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
                .FindByIdAsync(userId.ToString()))!.Email!;

        var client = factory.CreateAnonymousClient(slug);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Task<HttpResponseMessage> SetRole(HttpClient c, string prefix, Guid id, string role) =>
        c.PutAsJsonAsync($"/api/v1{prefix}/users/{id}/role", new { role });

    private static Task<HttpResponseMessage> SetActive(HttpClient c, string prefix, Guid id, bool isActive) =>
        c.PutAsJsonAsync($"/api/v1{prefix}/users/{id}/status", new { isActive });

    // ── Self-lockout ──────────────────────────────────────────────────────

    [Fact]
    public async Task ClinicOwner_CannotDeactivateThemselves()
    {
        var clinic = await ClinicAsync();
        var owner = await UserAsync(clinic, Roles.ClinicOwner);
        var client = await SignedInAsync(owner, clinic.Slug);

        (await SetActive(client, "", owner, false)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ClinicOwner_CannotChangeTheirOwnRole()
    {
        var clinic = await ClinicAsync();
        var owner = await UserAsync(clinic, Roles.ClinicOwner);
        var client = await SignedInAsync(owner, clinic.Slug);

        (await SetRole(client, "", owner, Roles.Receptionist)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SuperAdmin_CannotDeactivateOrDemoteThemselves()
    {
        var clinic = await ClinicAsync();
        var admin = await UserAsync(null, Roles.SuperAdmin);
        var client = await SignedInAsync(admin, clinic.Slug);

        (await SetActive(client, "/admin", admin, false)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRole(client, "/admin", admin, Roles.ReadOnly)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SuperAdmin_CannotDeactivateAClinicsLastOwner()
    {
        var clinic = await ClinicAsync();
        var owner = await UserAsync(clinic, Roles.ClinicOwner);
        var admin = await SignedInAsync(await UserAsync(null, Roles.SuperAdmin), clinic.Slug);

        (await SetActive(admin, "/admin", owner, false)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── ClinicAdmin vs ClinicOwner ────────────────────────────────────────

    [Fact]
    public async Task ClinicAdmin_CannotDemoteOrDeactivateTheOwner()
    {
        var clinic = await ClinicAsync();
        var owner = await UserAsync(clinic, Roles.ClinicOwner);
        var admin = await SignedInAsync(await UserAsync(clinic, Roles.ClinicAdmin), clinic.Slug);

        (await SetRole(admin, "", owner, Roles.Receptionist)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetActive(admin, "", owner, false)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ClinicAdmin_CannotPromoteAnyoneToOwner()
    {
        var clinic = await ClinicAsync();
        await UserAsync(clinic, Roles.ClinicOwner);
        var adminId = await UserAsync(clinic, Roles.ClinicAdmin);
        var receptionist = await UserAsync(clinic, Roles.Receptionist);
        var admin = await SignedInAsync(adminId, clinic.Slug);

        (await SetRole(admin, "", receptionist, Roles.ClinicOwner)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ClinicAdmin_CanStillManageOrdinaryStaff()
    {
        var clinic = await ClinicAsync();
        await UserAsync(clinic, Roles.ClinicOwner);
        var admin = await SignedInAsync(await UserAsync(clinic, Roles.ClinicAdmin), clinic.Slug);
        var receptionist = await UserAsync(clinic, Roles.Receptionist);

        (await SetRole(admin, "", receptionist, Roles.BillingStaff)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SetActive(admin, "", receptionist, false)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Owner_CanDemoteAnotherOwner_WhenOneRemains()
    {
        var clinic = await ClinicAsync();
        var owner = await SignedInAsync(await UserAsync(clinic, Roles.ClinicOwner), clinic.Slug);
        var second = await UserAsync(clinic, Roles.ClinicOwner);

        (await SetRole(owner, "", second, Roles.ClinicAdmin)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Platform role ─────────────────────────────────────────────────────

    [Fact]
    public async Task SuperAdmin_CannotMakeAClinicUserASuperAdmin()
    {
        var clinic = await ClinicAsync();
        var receptionist = await UserAsync(clinic, Roles.Receptionist);
        var admin = await SignedInAsync(await UserAsync(null, Roles.SuperAdmin), clinic.Slug);

        (await SetRole(admin, "/admin", receptionist, Roles.SuperAdmin)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
