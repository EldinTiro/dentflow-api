namespace DentFlow.Application.Common.Interfaces;

/// <summary>Staff profiles as other modules see them, without a reference to the Staff module.</summary>
public interface IStaffDirectory
{
    /// <summary>
    /// Renames the profile linked to this account in the current clinic, if there is one
    /// (people-merge plan D6: the profile is the source of truth for the name).
    /// </summary>
    Task RenameLinkedProfileAsync(Guid userId, string firstName, string lastName, CancellationToken ct);
}
