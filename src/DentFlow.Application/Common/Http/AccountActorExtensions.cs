using FastEndpoints;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Application.Common.Http;

public static class AccountActorExtensions
{
    /// <summary>The signed-in caller, for guards that care who acts on whose account.</summary>
    public static AccountActor AccountActor(this IEndpoint endpoint)
    {
        var user = endpoint.HttpContext.User;
        var id = Guid.TryParse(user.FindFirst("sub")?.Value, out var sub) ? sub : Guid.Empty;
        return new AccountActor(id, [.. user.FindAll("role").Select(c => c.Value)]);
    }
}
