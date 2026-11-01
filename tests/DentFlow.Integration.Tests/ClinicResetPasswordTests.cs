using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// People-merge plan 2.5: the clinic owner can reset a team member's password
/// (only SuperAdmin could before). Guards from QA-012 still apply.
/// </summary>
[Collection("Integration")]
public class ClinicResetPasswordTests(DentFlowAppFactory factory)
{
    private const string Password = "Reset123!@#abcd";
    private record LoginDto(string AccessToken, Guid UserId, bool MustChangePassword);
    private record ResetDto(string TempPassword);

    private async Task<(string Slug, Guid TenantId)> ClinicAsync()
    {
        var slug = $"reset-{Guid.NewGuid():N}"[..18];
        return (slug, await factory.SeedTenantAsync(slug, "Reset Clinic"));
    }

    private async Task<(Guid Id, string Email)> UserAsync(Guid tenantId, string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@reset.test";
        return (await factory.SeedUserAsync(email, Password, role, tenantId), email);
    }

    private async Task<(HttpClient Client, Guid UserId)> SignedInAsync(string slug, string email, string password = Password)
    {
        var client = factory.CreateAnonymousClient(slug);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await login.Content.ReadFromJsonAsync<LoginDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.AccessToken);
        return (client, dto.UserId);
    }

    [Fact]
    public async Task Owner_ResetsATeamMember_WhoMustChooseANewPasswordAtNextSignIn()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, ownerEmail) = await UserAsync(tenantId, Roles.ClinicOwner);
        var (dentistId, dentistEmail) = await UserAsync(tenantId, Roles.Staff);
        var (owner, _) = await SignedInAsync(slug, ownerEmail);

        var reset = await owner.PostAsync($"/api/v1/users/{dentistId}/reset-password", null);

        reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync());
        var temp = (await reset.Content.ReadFromJsonAsync<ResetDto>())!.TempPassword;
        var login = await factory.CreateAnonymousClient(slug).PostAsJsonAsync("/api/v1/auth/login", new { email = dentistEmail, password = temp });
        (await login.Content.ReadFromJsonAsync<LoginDto>())!.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Owner_ResettingAnotherClinicsUser_Returns404()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, otherTenant) = await ClinicAsync();
        var (_, ownerEmail) = await UserAsync(tenantId, Roles.ClinicOwner);
        var (strangerId, _) = await UserAsync(otherTenant, Roles.Staff);
        var (owner, _) = await SignedInAsync(slug, ownerEmail);

        (await owner.PostAsync($"/api/v1/users/{strangerId}/reset-password", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Owner_ResettingTheirOwnPassword_Returns409()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, ownerEmail) = await UserAsync(tenantId, Roles.ClinicOwner);
        var (owner, ownerId) = await SignedInAsync(slug, ownerEmail);

        var reset = await owner.PostAsync($"/api/v1/users/{ownerId}/reset-password", null);

        reset.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await reset.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("user.self");
    }

    [Fact]
    public async Task Receptionist_CannotResetPasswords()
    {
        var (slug, tenantId) = await ClinicAsync();
        var (_, receptionistEmail) = await UserAsync(tenantId, Roles.Staff);
        var (dentistId, _) = await UserAsync(tenantId, Roles.Staff);
        var (receptionist, _) = await SignedInAsync(slug, receptionistEmail);

        (await receptionist.PostAsync($"/api/v1/users/{dentistId}/reset-password", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
