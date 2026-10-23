using ErrorOr;

namespace DentFlow.Application.Common.Interfaces;

/// <summary>Who is making a change to someone's account, as other modules see it.</summary>
public record AccountActor(Guid UserId, IReadOnlyCollection<string> Roles);

/// <summary>
/// Account changes that follow a staff profile (people-merge plan 2.6 / 2.7).
/// Implemented in the Identity module so the user-management guards (self, last owner,
/// owner-only) stay in one place and are not copied.
/// </summary>
public interface IUserAccountLifecycle
{
    /// <summary>Deactivate (never delete) an account. Missing accounts are a no-op.</summary>
    Task<ErrorOr<Success>> DeactivateAsync(Guid userId, AccountActor actor, CancellationToken ct);

    /// <summary>Keep the account's name equal to its staff profile's (D6). Missing accounts are a no-op.</summary>
    Task<ErrorOr<Success>> RenameAsync(Guid userId, string firstName, string lastName, CancellationToken ct);
}
