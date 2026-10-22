using FastEndpoints;
using DentFlow.Identity.Application.Commands;

namespace DentFlow.Identity.Endpoints;

internal static class UserManagementActorExtensions
{
    /// <summary>The signed-in caller, as the user-management guards need it.</summary>
    public static UserManagementActor Actor(this IEndpoint endpoint)
    {
        var user = endpoint.HttpContext.User;
        var id = Guid.TryParse(user.FindFirst("sub")?.Value, out var sub) ? sub : Guid.Empty;
        return new UserManagementActor(id, [.. user.FindAll("role").Select(c => c.Value)]);
    }
}
