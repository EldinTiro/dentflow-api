using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Identity.Endpoints;

/// <summary>
/// Ends the session server-side by revoking the refresh token family, then clears the
/// cookie. Anonymous because a client whose access token has already expired must still
/// be able to log out.
/// </summary>
[AllowAnonymous]
public class LogoutEndpoint(IRefreshTokenService refreshTokens) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/logout");
        AllowAnonymous();
        Version(1);
        Summary(s =>
        {
            s.Summary = "Log out";
            s.Description = "Revokes the refresh token family and clears the cookie.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var presented = AuthTokens.ReadRefreshCookie(HttpContext.Request);

        if (presented is not null)
            await refreshTokens.RevokeAsync(presented, ct);

        AuthTokens.ClearRefreshCookie(HttpContext.Response);

        // Always 204: whether or not a cookie was presented, the caller ends up logged out.
        await SendNoContentAsync(ct);
    }
}
