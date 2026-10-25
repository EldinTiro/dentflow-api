namespace DentFlow.Application.Common.Interfaces;

/// <summary>Whether a staff member can take appointments (people-merge D3/D11).</summary>
public record StaffBookability(bool IsActive, bool IsBookable);

/// <summary>Staff profiles as other modules see them, without a reference to the Staff module.</summary>
public interface IStaffDirectory
{
    /// <summary>
    /// Renames the profile linked to this account in the current clinic, if there is one
    /// (people-merge plan D6: the profile is the source of truth for the name).
    /// </summary>
    Task RenameLinkedProfileAsync(Guid userId, string firstName, string lastName, CancellationToken ct);

    /// <summary>The staff member in the caller's clinic, or null when there is none (DT-02).</summary>
    Task<StaffBookability?> GetBookabilityAsync(Guid staffId, CancellationToken ct);
}
