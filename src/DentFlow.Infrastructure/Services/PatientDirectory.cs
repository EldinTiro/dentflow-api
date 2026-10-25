using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Patients.Domain;

namespace DentFlow.Infrastructure.Services;

internal sealed class PatientDirectory(ApplicationDbContext db) : IPatientDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> patientIds, CancellationToken ct)
    {
        var ids = patientIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        return await db.Set<Patient>()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FirstName + " " + p.LastName, ct);
    }

    public Task<bool> IsInactiveAsync(Guid patientId, CancellationToken ct) =>
        db.Set<Patient>().AnyAsync(p => p.Id == patientId && p.Status == PatientStatus.Inactive, ct);
}
