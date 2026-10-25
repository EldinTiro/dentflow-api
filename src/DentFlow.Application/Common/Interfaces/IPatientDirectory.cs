namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Patient display names for other modules (e.g. the invoice list) without a
/// reference to the Patients module. Tenant-scoped like any other read.
/// </summary>
public interface IPatientDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> patientIds, CancellationToken ct);

    /// <summary>
    /// True only for a patient of this clinic who is deactivated (people-merge plan D11).
    /// An unknown id is not "inactive" — callers that need existence check it themselves.
    /// </summary>
    Task<bool> IsInactiveAsync(Guid patientId, CancellationToken ct);

    /// <summary>Whether the patient exists (not deleted) in the caller's clinic.</summary>
    Task<bool> ExistsAsync(Guid patientId, CancellationToken ct);
}
