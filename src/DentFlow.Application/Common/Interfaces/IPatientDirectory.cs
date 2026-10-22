namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Patient display names for other modules (e.g. the invoice list) without a
/// reference to the Patients module. Tenant-scoped like any other read.
/// </summary>
public interface IPatientDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> patientIds, CancellationToken ct);
}
