using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Refresh token issuance, rotation and reuse detection.
///
/// Cookies are handled manually (HandleCookies = false) so a test can replay an
/// already-rotated token — the whole point of reuse detection. An HTTP client that
/// quietly manages the cookie jar for you cannot express that.
/// </summary>
[Collection("Integration")]
public class RefreshTokenTests(DentFlowAppFactory factory)
{
    private const string Slug = "refreshclinic";
    private const string Password = "RefreshTest123!@#";

    private record LoginResponse(string AccessToken, int ExpiresIn, Guid UserId, string Email);

    private async Task<HttpClient> ClinicClientAsync(string email)
    {
        var tenantId = await factory.SeedTenantAsync(Slug, "Refresh Clinic");
        await factory.SeedUserAsync(email, Password, "ClinicOwner", tenantId);
        return factory.CreateAnonymousClient(Slug);
    }

    /// <summary>Reads the refresh cookie value out of Set-Cookie.</summary>
    private static string? RefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;

        foreach (var cookie in cookies)
        {
            if (!cookie.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase)) continue;
            var value = cookie["refresh_token=".Length..].Split(';')[0];
            return string.IsNullOrEmpty(value) ? null : value;
        }
        return null;
    }

    private static string? RawSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var c)
            ? c.FirstOrDefault(x => x.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase))
            : null;

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email) =>
        await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

    private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client, string? cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        if (cookie is not null) request.Headers.Add("Cookie", $"refresh_token={cookie}");
        return await client.SendAsync(request);
    }

    // ── Issuance ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_IssuesHttpOnlyRefreshCookie()
    {
        var email = $"issue-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var response = await LoginAsync(client, email);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setCookie = RawSetCookie(response);
        setCookie.Should().NotBeNull("login must issue a refresh token");
        setCookie.Should().Contain("httponly", "the refresh token must be unreachable from JS");
        setCookie.Should().Contain("path=/api/v1/auth", "scope the cookie to the auth endpoints");
        setCookie!.ToLowerInvariant().Should().Contain("samesite");
    }

    [Fact]
    public async Task Login_StoresOnlyAHashOfTheRefreshToken()
    {
        var email = $"hash-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var response = await LoginAsync(client, email);
        var cookie = RefreshCookie(response);
        cookie.Should().NotBeNull();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var stored = await db.RefreshTokens.Select(t => t.TokenHash).ToListAsync();

        stored.Should().NotContain(cookie,
            "a stolen database must not hand over usable refresh tokens");
    }

    // ── Rotation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsNewAccessTokenAndRotatesCookie()
    {
        var email = $"rotate-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var login = await LoginAsync(client, email);
        var firstCookie = RefreshCookie(login);
        var firstAccess = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        var refreshed = await RefreshAsync(client, firstCookie);

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await refreshed.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.AccessToken.Should().NotBe(firstAccess, "refresh must mint a fresh access token");

        var secondCookie = RefreshCookie(refreshed);
        secondCookie.Should().NotBeNull("refresh must rotate the refresh token");
        secondCookie.Should().NotBe(firstCookie, "the rotated token must be a new value");
    }

    [Fact]
    public async Task AccessTokenFromRefresh_IsAcceptedByAProtectedEndpoint()
    {
        var email = $"usable-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var login = await LoginAsync(client, email);
        var refreshed = await RefreshAsync(client, RefreshCookie(login));
        var access = (await refreshed.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        var probe = factory.CreateAnonymousClient(Slug);
        probe.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);

        var response = await probe.GetAsync("/api/v1/patients?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a refreshed access token must carry the same roles and tenant as the original");
    }

    [Fact]
    public async Task Refresh_WithAlreadyRotatedToken_IsRejected()
    {
        var email = $"stale-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var login = await LoginAsync(client, email);
        var firstCookie = RefreshCookie(login);

        await RefreshAsync(client, firstCookie);          // rotates it away
        var replay = await RefreshAsync(client, firstCookie);

        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "a token that has already been exchanged must not work twice");
    }

    // ── Reuse detection ───────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ReusingRotatedToken_RevokesTheWholeFamily()
    {
        var email = $"reuse-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var login = await LoginAsync(client, email);
        var stolen = RefreshCookie(login);

        // Legitimate holder rotates twice; the chain is now two tokens deep.
        var second = await RefreshAsync(client, stolen);
        var liveCookie = RefreshCookie(second);
        liveCookie.Should().NotBeNull();

        // Attacker replays the original, proving the token leaked.
        var attack = await RefreshAsync(client, stolen);
        attack.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The legitimate token must now be dead too — that is the point of family revocation.
        var afterBreach = await RefreshAsync(client, liveCookie);
        afterBreach.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "detecting reuse means the family is compromised, so every token in it must be revoked");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.UserId;

        var live = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .CountAsync();

        live.Should().Be(0, "no token of a compromised family may remain active");
    }

    // ── Rejection paths ───────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var client = await ClinicClientAsync($"nocookie-{Guid.NewGuid():N}@clinic.com");

        var response = await RefreshAsync(client, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var client = await ClinicClientAsync($"unknown-{Guid.NewGuid():N}@clinic.com");

        var response = await RefreshAsync(client, "not-a-real-token-value");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Logout ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var email = $"logout-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var login = await LoginAsync(client, email);
        var cookie = RefreshCookie(login);

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("Cookie", $"refresh_token={cookie}");
        var logoutResponse = await client.SendAsync(logout);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterLogout = await RefreshAsync(client, cookie);
        afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "logging out must make the refresh token unusable");
    }

    [Fact]
    public async Task Logout_ClearsTheCookie()
    {
        var email = $"clear-{Guid.NewGuid():N}@clinic.com";
        var client = await ClinicClientAsync(email);

        var login = await LoginAsync(client, email);

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("Cookie", $"refresh_token={RefreshCookie(login)}");
        var response = await client.SendAsync(logout);

        RawSetCookie(response).Should().NotBeNull("logout must send an expiring cookie back");
        RefreshCookie(response).Should().BeNull("the cookie value must be emptied");
    }
}
