using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

internal sealed class PatientHistoryReader(ApplicationDbContext db) : IPatientHistoryReader
{
    public async Task<bool> HasHistoryAsync(Guid patientId, bool missedVisitsAreHistory, CancellationToken ct) =>
        await db.Appointments.AnyAsync(a => a.PatientId == patientId
            && (missedVisitsAreHistory || (a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)), ct) ||
        await db.TreatmentPlans.AnyAsync(p => p.PatientId == patientId, ct) ||
        await db.Invoices.AnyAsync(i => i.PatientId == patientId, ct) ||
        await db.PatientDocuments.AnyAsync(d => d.PatientId == patientId, ct);

    public async Task RemoveMissedVisitsAsync(Guid patientId, CancellationToken ct)
    {
        var missed = await db.Appointments
            .Where(a => a.PatientId == patientId && (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.NoShow))
            .ToListAsync(ct);
        foreach (var visit in missed) visit.SoftDelete();
        await db.SaveChangesAsync(ct);
    }
}
