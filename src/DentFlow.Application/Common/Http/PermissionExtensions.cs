using FastEndpoints;
using DentFlow.Domain.Identity;

namespace DentFlow.Application.Common.Http;

public static class PermissionExtensions
{
    /// <summary>Whether the caller holds a permission through any of their roles.</summary>
    public static bool Can(this IEndpoint endpoint, string permission) =>
        endpoint.HttpContext.User.FindAll("role").Any(c => RolePermissions.Has(c.Value, permission));
}
