using Finbuckle.MultiTenant.Abstractions;
using DentFlow.Domain.Identity;
using DentFlow.Domain.Tenants;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.API.Middleware;

/// <summary>
/// Rejects authenticated requests for a deactivated clinic with 403, so access tokens
/// issued before the deactivation stop working right away rather than at expiry.
/// Auth endpoints are left alone — login and refresh enforce suspension themselves,
/// and logout must keep working.
/// </summary>
public sealed class SuspendedTenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IMultiTenantContextAccessor tenantAccessor, ApplicationDbContext db)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true
            && !user.IsInRole(Roles.SuperAdmin)
            && !context.Request.Path.StartsWithSegments("/api/v1/auth")
            && Guid.TryParse(tenantAccessor.MultiTenantContext?.TenantInfo?.Id, out var tenantId)
            && await db.IsTenantSuspendedAsync(tenantId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status403Forbidden,
                errors = new[] { new { name = TenantErrors.Inactive.Code, reason = TenantErrors.Inactive.Description } },
            }, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
