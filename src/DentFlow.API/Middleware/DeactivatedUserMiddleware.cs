using Microsoft.EntityFrameworkCore;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.API.Middleware;

/// <summary>
/// DT-17: a deactivated user's access token stops working at once, like a suspended
/// clinic's, instead of at expiry. 401, so the web client tries a refresh — which an
/// inactive account can't get — and signs out. Auth endpoints are left alone so logout
/// still works; tokens for users not in the database are left to the normal checks.
/// </summary>
public sealed class DeactivatedUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.Request.Path.StartsWithSegments("/api/v1/auth")
            && Guid.TryParse(context.User.FindFirst("sub")?.Value, out var userId)
            && await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && !u.IsActive, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status401Unauthorized,
                errors = new[] { new { name = IdentityErrors.AccountInactive.Code, reason = IdentityErrors.AccountInactive.Description } },
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
