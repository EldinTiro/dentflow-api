using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Identity.Endpoints;

[AllowAnonymous]
public class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    IConfiguration configuration,
    IRefreshTokenService refreshTokens)
    : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Version(1);
        Summary(s =>
        {
            s.Summary = "Login";
            s.Description = "Authenticate with email and password to receive a JWT access token.";
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);

        if (user is null || !user.IsActive)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, req.Password);
        if (!passwordValid)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var roles = await userManager.GetRolesAsync(user);

        var (token, expiresInSeconds) =
            await AuthTokens.CreateAccessTokenAsync(user, userManager, configuration);

        // Refresh token in an HttpOnly cookie: the access token is short-lived, and
        // without this the session simply dies when it expires.
        var refresh = await refreshTokens.IssueAsync(
            user.Id, user.TenantId,
            AuthTokens.ClientIp(HttpContext), AuthTokens.UserAgent(HttpContext.Request), ct);

        AuthTokens.SetRefreshCookie(HttpContext.Response, refresh.Token, refresh.ExpiresAt);

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var language = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.Language)
            .FirstOrDefaultAsync(ct) ?? "en";

        await SendOkAsync(new LoginResponse(
            AccessToken: token,
            ExpiresIn: expiresInSeconds,
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: [.. roles],
            Language: language
        ), ct);
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    Guid UserId,
    string Email,
    string FullName,
    List<string> Roles,
    string Language);

