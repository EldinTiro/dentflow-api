using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-011: a temporary password (invite, provision, admin reset) gave full access
/// indefinitely. The user must now replace it before using anything else.
/// </summary>
[Collection("Integration")]
public class TemporaryPasswordTests(DentFlowAppFactory factory)
{
    private const string OwnerPassword = "Owner123!@#abc";
    private const string NewPassword = "MyOwnSecret123!@#";

    private record InviteDto(Guid UserId, string Email, string TempPassword);
    private record LoginDto(string AccessToken, Guid UserId, bool MustChangePassword);

    private async Task<(string Slug, HttpClient Owner)> ClinicAsync()
    {
        var slug = $"tmp-{Guid.NewGuid():N}"[..18];
        var tenantId = await factory.SeedTenantAsync(slug, "Temp Clinic");
        var email = $"owner-{slug}@test.com";
        await factory.SeedUserAsync(email, OwnerPassword, Roles.ClinicOwner, tenantId);
        var owner = factory.CreateAnonymousClient(slug);
        var login = await LoginAsync(owner, email, OwnerPassword);
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return (slug, owner);
    }

    private static async Task<LoginDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LoginDto>())!;
    }

    private async Task<(HttpClient Client, InviteDto Invite, LoginDto Login)> InvitedUserSignedInAsync(string role)
    {
        var (slug, owner) = await ClinicAsync();
        var invite = await owner.PostAsJsonAsync("/api/v1/users/invite", new
        {
            email = $"new-{Guid.NewGuid():N}@test.com", firstName = "New", lastName = "Hire", role
        });
        invite.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var dto = (await invite.Content.ReadFromJsonAsync<InviteDto>())!;

        var client = factory.CreateAnonymousClient(slug);
        var login = await LoginAsync(client, dto.Email, dto.TempPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return (client, dto, login);
    }

    [Fact]
    public async Task OrdinaryLogin_DoesNotRequireAPasswordChange()
    {
        var (_, owner) = await ClinicAsync();

        (await owner.GetAsync("/api/v1/patients")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvitedUser_IsToldToChangeThePassword_AndIsBlockedUntilThen()
    {
        var (client, _, login) = await InvitedUserSignedInAsync(Roles.Receptionist);

        login.MustChangePassword.Should().BeTrue();
        var blocked = await client.GetAsync("/api/v1/patients");
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blocked.Content.ReadAsStringAsync()).Should().Contain("Auth.PasswordChangeRequired");
        (await client.GetAsync("/api/v1/users/me")).StatusCode.Should().Be(HttpStatusCode.OK,
            "the app still needs the profile to render the change-password screen");
    }

    [Fact]
    public async Task AfterChangingThePassword_TheUserGetsFullAccess()
    {
        var (client, invite, _) = await InvitedUserSignedInAsync(Roles.Hygienist);

        var change = await client.PostAsJsonAsync("/api/v1/users/me/change-password",
            new { currentPassword = invite.TempPassword, newPassword = NewPassword });
        change.StatusCode.Should().Be(HttpStatusCode.NoContent, await change.Content.ReadAsStringAsync());

        var relogin = await LoginAsync(client, invite.Email, NewPassword);
        relogin.MustChangePassword.Should().BeFalse();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", relogin.AccessToken);
        (await client.GetAsync("/api/v1/patients")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminReset_RequiresAChangeAgain()
    {
        var (slug, _) = await ClinicAsync();
        var userEmail = $"reset-{Guid.NewGuid():N}@test.com";
        var userId = await factory.SeedUserAsync(userEmail, OwnerPassword, Roles.Dentist,
            await factory.SeedTenantAsync(slug, "Temp Clinic"));
        var adminEmail = $"sa-{Guid.NewGuid():N}@test.com";
        await factory.SeedUserAsync(adminEmail, OwnerPassword, Roles.SuperAdmin, null);
        var admin = factory.CreateAnonymousClient(slug);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await LoginAsync(admin, adminEmail, OwnerPassword)).AccessToken);

        var reset = await admin.PostAsync($"/api/v1/admin/users/{userId}/reset-password", null);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        var temp = JsonDocument.Parse(await reset.Content.ReadAsStringAsync()).RootElement.GetProperty("tempPassword").GetString()!;

        (await LoginAsync(factory.CreateAnonymousClient(slug), userEmail, temp)).MustChangePassword.Should().BeTrue();
    }
}
