using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

internal sealed class PatientHistoryReader(ApplicationDbContext db) : IPatientHistoryReader
{
    public async Task<bool> HasHistoryAsync(Guid patientId, CancellationToken ct) =>
        await db.Appointments.AnyAsync(a => a.PatientId == patientId, ct) ||
        await db.TreatmentPlans.AnyAsync(p => p.PatientId == patientId, ct) ||
        await db.Invoices.AnyAsync(i => i.PatientId == patientId, ct) ||
        await db.PatientDocuments.AnyAsync(d => d.PatientId == patientId, ct);
}
