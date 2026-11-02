using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// User report 2026-11-02: Lejla's sign-in email was wrong and nothing could change it —
/// the profile email is locked while it is the sign-in email (DT-20). The Administrator
/// changes it on the account, and the profile follows, in one go.
/// </summary>
[Collection("Integration")]
public class ChangeSignInEmailTests(DentFlowAppFactory factory)
{
    private const string Password = "Email123!@#abcd";
    private record LoginDto(string AccessToken, Guid UserId);
    private record CreatedStaffDto(Guid Id, string TempPassword);
    private record AccessDto(Guid UserId);
    private record StaffDto(Guid Id, string? Email, AccessDto? Access);

    private async Task<(string Slug, HttpClient Admin)> ClinicWithAdminAsync()
    {
        var slug = $"email-{Guid.NewGuid():N}"[..18];
        var tenantId = await factory.SeedTenantAsync(slug, "Email Clinic");
        var adminEmail = $"admin-{Guid.NewGuid():N}@email.test";
        await factory.SeedUserAsync(adminEmail, Password, Roles.ClinicOwner, tenantId);
        return (slug, await SignedInAsync(slug, adminEmail, Password));
    }

    private async Task<HttpClient> SignedInAsync(string slug, string email, string password)
    {
        var client = factory.CreateAnonymousClient(slug);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<LoginDto>())!.AccessToken);
        return client;
    }

    /// <summary>A colleague added with sign-in access, as the Staff screen does it.</summary>
    private static async Task<(Guid StaffId, Guid UserId, string TempPassword)> ColleagueAsync(HttpClient admin, string email)
    {
        var created = await admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Lejla", lastName = "Drino", email, access = new { role = Roles.Staff },
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var dto = (await created.Content.ReadFromJsonAsync<CreatedStaffDto>())!;
        var staff = (await admin.GetFromJsonAsync<StaffDto>($"/api/v1/staff/{dto.Id}"))!;
        return (dto.Id, staff.Access!.UserId, dto.TempPassword);
    }

    [Fact]
    public async Task Admin_ChangesASignInEmail_ThePersonSignsInWithTheNewOne_AndTheProfileFollows()
    {
        var (slug, admin) = await ClinicWithAdminAsync();
        var wrong = $"lejla-wrong-{Guid.NewGuid():N}@email.test";
        var right = $"Lejla.Drino-{Guid.NewGuid():N}@Email.test ";
        var (staffId, userId, temp) = await ColleagueAsync(admin, wrong);

        var response = await admin.PutAsJsonAsync($"/api/v1/users/{userId}/email", new { email = right });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var anonymous = factory.CreateAnonymousClient(slug);
        (await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = right.Trim(), password = temp }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = wrong, password = temp }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await admin.GetFromJsonAsync<StaffDto>($"/api/v1/staff/{staffId}"))!.Email.Should().Be(right.Trim());
    }

    [Fact]
    public async Task AnEmailAnotherAccountUses_Returns409_AndChangesNothing()
    {
        var (_, admin) = await ClinicWithAdminAsync();
        var taken = $"taken-{Guid.NewGuid():N}@email.test";
        await ColleagueAsync(admin, taken);
        var original = $"original-{Guid.NewGuid():N}@email.test";
        var (staffId, userId, _) = await ColleagueAsync(admin, original);

        var response = await admin.PutAsJsonAsync($"/api/v1/users/{userId}/email", new { email = taken.ToUpperInvariant() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("EmailAlreadyExists");
        (await admin.GetFromJsonAsync<StaffDto>($"/api/v1/staff/{staffId}"))!.Email.Should().Be(original);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task AnInvalidEmail_Returns400(string email)
    {
        var (_, admin) = await ClinicWithAdminAsync();
        var (_, userId, _) = await ColleagueAsync(admin, $"valid-{Guid.NewGuid():N}@email.test");

        (await admin.PutAsJsonAsync($"/api/v1/users/{userId}/email", new { email }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Staff_CannotChangeAnybodysSignInEmail()
    {
        var (slug, admin) = await ClinicWithAdminAsync();
        var (_, userId, _) = await ColleagueAsync(admin, $"target-{Guid.NewGuid():N}@email.test");
        var staffEmail = $"staff-{Guid.NewGuid():N}@email.test";
        var (_, _, staffTemp) = await ColleagueAsync(admin, staffEmail);
        var staff = await SignedInAsync(slug, staffEmail, staffTemp);

        (await staff.PutAsJsonAsync($"/api/v1/users/{userId}/email", new { email = $"x-{Guid.NewGuid():N}@email.test" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnotherClinicsAccount_Returns404()
    {
        var (_, admin) = await ClinicWithAdminAsync();
        var (_, otherAdmin) = await ClinicWithAdminAsync();
        var (_, strangerId, _) = await ColleagueAsync(otherAdmin, $"stranger-{Guid.NewGuid():N}@email.test");

        (await admin.PutAsJsonAsync($"/api/v1/users/{strangerId}/email", new { email = $"x-{Guid.NewGuid():N}@email.test" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
