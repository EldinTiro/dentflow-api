namespace DentFlow.Application.Common.Interfaces;

/// <summary>What a staff profile shows about its sign-in account (people-merge plan 2.4).</summary>
public record StaffAccessSummary(
    Guid UserId,
    string? Role,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLoginAt);

/// <summary>
/// Sign-in accounts as other modules see them, without a reference to the Identity
/// module (same pattern as <see cref="IPatientDirectory"/>).
/// </summary>
public interface IUserAccountDirectory
{
    /// <summary>One query for the whole set; ids without an account are left out.</summary>
    Task<IReadOnlyDictionary<Guid, StaffAccessSummary>> GetSummariesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
}
