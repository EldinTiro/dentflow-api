using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Hard deletes for the Administrator (user request 2026-11-13). The caller has already found
/// the person through the tenant-filtered repositories, so everything here is reached through
/// that person's id. Query filters are lifted only to take soft-deleted rows along too (a
/// deleted allergy, a cancelled visit removed earlier), never to reach another clinic.
/// </summary>
internal sealed class PeoplePurger(ApplicationDbContext db) : IPeoplePurger
{
    public async Task<IReadOnlyList<string>> PurgePatientAsync(Guid patientId, CancellationToken ct)
    {
        var files = await db.PatientDocuments.IgnoreQueryFilters()
            .Where(d => d.PatientId == patientId).Select(d => d.StorageKey).ToListAsync(ct);

        var visits = db.Appointments.IgnoreQueryFilters().Where(a => a.PatientId == patientId).Select(a => a.Id);
        await db.AppointmentStatusHistories.IgnoreQueryFilters().Where(h => visits.Contains(h.AppointmentId)).ExecuteDeleteAsync(ct);
        await db.NotificationLogs.IgnoreQueryFilters().Where(n => n.PatientId == patientId).ExecuteDeleteAsync(ct);

        var invoices = db.Invoices.IgnoreQueryFilters().Where(i => i.PatientId == patientId).Select(i => i.Id);
        await db.InvoicePayments.IgnoreQueryFilters().Where(p => invoices.Contains(p.InvoiceId)).ExecuteDeleteAsync(ct);
        await db.InvoiceLineItems.IgnoreQueryFilters().Where(l => invoices.Contains(l.InvoiceId)).ExecuteDeleteAsync(ct);
        await db.Invoices.IgnoreQueryFilters().Where(i => i.PatientId == patientId).ExecuteDeleteAsync(ct);

        await db.Appointments.IgnoreQueryFilters().Where(a => a.PatientId == patientId).ExecuteDeleteAsync(ct);

        var plans = db.TreatmentPlans.IgnoreQueryFilters().Where(p => p.PatientId == patientId).Select(p => p.Id);
        await db.TreatmentPlanItems.IgnoreQueryFilters().Where(i => plans.Contains(i.TreatmentPlanId)).ExecuteDeleteAsync(ct);
        await db.TreatmentPlans.IgnoreQueryFilters().Where(p => p.PatientId == patientId).ExecuteDeleteAsync(ct);

        await db.PatientDocuments.IgnoreQueryFilters().Where(d => d.PatientId == patientId).ExecuteDeleteAsync(ct);
        await db.ToothFindings.IgnoreQueryFilters().Where(f => f.PatientId == patientId).ExecuteDeleteAsync(ct);
        await db.Allergies.IgnoreQueryFilters().Where(a => a.PatientId == patientId).ExecuteDeleteAsync(ct);
        await db.MedicalHistories.IgnoreQueryFilters().Where(m => m.PatientId == patientId).ExecuteDeleteAsync(ct);
        await db.PatientEmergencyContacts.IgnoreQueryFilters().Where(c => c.PatientId == patientId).ExecuteDeleteAsync(ct);

        // Patients they referred stay, without the link
        await db.Patients.IgnoreQueryFilters().Where(p => p.ReferredByPatientId == patientId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReferredByPatientId, (Guid?)null), ct);

        var userId = await db.Patients.IgnoreQueryFilters().Where(p => p.Id == patientId).Select(p => p.UserId).FirstOrDefaultAsync(ct);
        await db.Patients.IgnoreQueryFilters().Where(p => p.Id == patientId).ExecuteDeleteAsync(ct);
        if (userId is { } uid) await DeleteAccountAsync(uid, ct);

        return files;
    }

    public async Task PurgeStaffMemberAsync(Guid staffMemberId, Guid? userId, CancellationToken ct)
    {
        await db.Patients.IgnoreQueryFilters().Where(p => p.PreferredProviderId == staffMemberId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PreferredProviderId, (Guid?)null), ct);
        await db.MedicalHistories.IgnoreQueryFilters().Where(m => m.RecordedByStaffId == staffMemberId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.RecordedByStaffId, (Guid?)null), ct);

        await db.StaffAvailabilities.IgnoreQueryFilters().Where(a => a.StaffMemberId == staffMemberId).ExecuteDeleteAsync(ct);
        await db.StaffBlockedTimes.IgnoreQueryFilters().Where(b => b.StaffMemberId == staffMemberId).ExecuteDeleteAsync(ct);
        await db.StaffMembers.IgnoreQueryFilters().Where(s => s.Id == staffMemberId).ExecuteDeleteAsync(ct);

        if (userId is { } uid) await DeleteAccountAsync(uid, ct);
    }

    /// <summary>The sign-in goes with the person: sessions, preferences, roles, the account.</summary>
    private async Task DeleteAccountAsync(Guid userId, CancellationToken ct)
    {
        await db.RefreshTokens.IgnoreQueryFilters().Where(t => t.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserPreferences.IgnoreQueryFilters().Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await db.UserRoles.Where(r => r.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(ct);
    }
}
