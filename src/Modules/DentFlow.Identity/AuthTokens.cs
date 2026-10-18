using FastEndpoints.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using DentFlow.Domain.Identity;

namespace DentFlow.Identity;

/// <summary>
/// Shared between login and refresh: both mint the same kind of access token and set
/// the same refresh cookie, and a difference between them would be a security bug.
/// </summary>
internal static class AuthTokens
{
    public const string RefreshCookieName = "refresh_token";

    /// <summary>
    /// Scoped to the auth endpoints — the cookie has no business being attached to
    /// every API call, and a narrower path shrinks what a CSRF attempt can reach.
    /// </summary>
    private const string RefreshCookiePath = "/api/v1/auth";

    public static async Task<(string Token, int ExpiresInSeconds)> CreateAccessTokenAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var roles = await userManager.GetRolesAsync(user);

        var signingKey = configuration["Jwt:SigningKey"]!;
        var expiryMinutes = int.TryParse(configuration["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 15;

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = signingKey;
            o.ExpireAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            o.User.Roles.AddRange(roles);
            o.User.Claims.Add(("sub", user.Id.ToString()));
            // Without jti the payload is fully deterministic and exp/iat are
            // second-granular, so two tokens minted in the same second are byte
            // identical — indistinguishable in logs and impossible to revoke singly.
            o.User.Claims.Add(("jti", Guid.NewGuid().ToString()));
            o.User.Claims.Add(("email", user.Email!));
            o.User.Claims.Add(("name", user.FullName));
            if (user.TenantId.HasValue)
                o.User.Claims.Add(("tid", user.TenantId.Value.ToString()));
        });

        return (token, expiryMinutes * 60);
    }

    public static void SetRefreshCookie(HttpResponse response, string token, DateTime expiresAt) =>
        response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expiresAt,
        });

    public static void ClearRefreshCookie(HttpResponse response) =>
        response.Cookies.Append(RefreshCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UnixEpoch,
        });

    public static string? ReadRefreshCookie(HttpRequest request) =>
        request.Cookies.TryGetValue(RefreshCookieName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    public static string? ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    public static string? UserAgent(HttpRequest request) =>
        request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
