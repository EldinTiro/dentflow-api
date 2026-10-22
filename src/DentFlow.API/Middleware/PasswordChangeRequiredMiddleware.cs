using DentFlow.Domain.Identity;

namespace DentFlow.API.Middleware;

/// <summary>
/// A token issued for a temporary password only reaches the auth endpoints and the
/// caller's own profile — enough to render the change-password screen and submit it.
/// Everything else answers 403 <c>Auth.PasswordChangeRequired</c> (QA-011).
/// </summary>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (context.User.HasClaim(IdentityClaims.PasswordChangeRequired, "true")
            && !path.StartsWithSegments("/api/v1/auth")
            && !path.StartsWithSegments("/api/v1/users/me"))
        {
            var error = IdentityErrors.PasswordChangeRequired;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status403Forbidden,
                errors = new[] { new { name = error.Code, reason = error.Description } },
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
