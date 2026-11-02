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

    /// <summary>
    /// Gives the profile linked to this account its new sign-in email (the profile email is the
    /// sign-in email while linked, DT-20). False when another profile in the clinic already has
    /// it; true when there is no linked profile.
    /// </summary>
    Task<bool> ChangeLinkedProfileEmailAsync(Guid userId, string email, CancellationToken ct);

    /// <summary>The staff member in the caller's clinic, or null when there is none (DT-02).</summary>
    Task<StaffBookability?> GetBookabilityAsync(Guid staffId, CancellationToken ct);

    /// <summary>
    /// DT-07: gives a new account its staff profile in <paramref name="tenantId"/> — links an
    /// unlinked profile with the same email, or creates one. Accounts made outside the Staff
    /// screen (SuperAdmin provisioning, invite, tenant creation) need this to be visible.
    /// </summary>
    Task CreateLinkedProfileAsync(Guid userId, Guid tenantId, string firstName, string lastName, string email, string role, CancellationToken ct);
}
