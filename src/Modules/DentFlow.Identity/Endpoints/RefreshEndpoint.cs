using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Identity.Endpoints;

/// <summary>
/// Exchanges the refresh cookie for a new access token and rotates the cookie.
/// Anonymous by design: the expired access token cannot authenticate this call —
/// the cookie is the credential.
/// </summary>
[AllowAnonymous]
public class RefreshEndpoint(
    IRefreshTokenService refreshTokens,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    IConfiguration configuration)
    : EndpointWithoutRequest<RefreshResponse>
{
    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Version(1);
        Summary(s =>
        {
            s.Summary = "Refresh the access token";
            s.Description = "Reads the HttpOnly refresh cookie, rotates it, and returns a new access token.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var presented = AuthTokens.ReadRefreshCookie(HttpContext.Request);
        if (presented is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var rotation = await refreshTokens.RotateAsync(
            presented, AuthTokens.ClientIp(HttpContext), AuthTokens.UserAgent(HttpContext.Request), ct);

        if (rotation.IsError)
        {
            // Rotation failures are all "this cookie is no longer usable"; the client's
            // only correct response is to log in again. Clear it so a dead cookie is not
            // replayed on every page load.
            AuthTokens.ClearRefreshCookie(HttpContext.Response);
            await SendUnauthorizedAsync(ct);
            return;
        }

        var user = await userManager.FindByIdAsync(rotation.Value.UserId.ToString());
        if (user is null || !user.IsActive || await db.IsTenantSuspendedAsync(user.TenantId, ct))
        {
            await refreshTokens.RevokeAsync(rotation.Value.Token, ct);
            AuthTokens.ClearRefreshCookie(HttpContext.Response);
            await SendUnauthorizedAsync(ct);
            return;
        }

        var (accessToken, expiresIn) =
            await AuthTokens.CreateAccessTokenAsync(user, userManager, configuration);

        AuthTokens.SetRefreshCookie(
            HttpContext.Response, rotation.Value.Token, rotation.Value.ExpiresAt);

        var roles = await userManager.GetRolesAsync(user);
        var language = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.Language)
            .FirstOrDefaultAsync(ct) ?? "en";

        await SendOkAsync(new RefreshResponse(
            AccessToken: accessToken,
            ExpiresIn: expiresIn,
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: [.. roles],
            Language: language,
            MustChangePassword: user.MustChangePassword
        ), ct);
    }
}

public record RefreshResponse(
    string AccessToken,
    int ExpiresIn,
    Guid UserId,
    string Email,
    string FullName,
    List<string> Roles,
    string Language,
    bool MustChangePassword);
