using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Domain;
using DentFlow.Billing.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// The per-patient facts of the patient list, read across modules. Every set here is tenant
/// filtered by the DbContext, so a clinic only ever sees its own patients.
/// </summary>
internal sealed class PatientInsightsReader(ApplicationDbContext db) : IPatientInsightsReader
{
    private static readonly string[] Open = [AppointmentStatus.Scheduled, AppointmentStatus.CheckedIn, AppointmentStatus.InProgress];

    /// <summary>Most serious first. Severity is free text on the allergy; unknown values rank last.</summary>
    private static readonly string[] SeverityRank = ["LifeThreatening", "Severe", "Moderate", "Mild"];

    public async Task<IReadOnlyDictionary<Guid, PatientInsights>> GetAsync(
        IReadOnlyCollection<Guid> patientIds, DateTime now, CancellationToken ct)
    {
        if (patientIds.Count == 0) return new Dictionary<Guid, PatientInsights>();
        var ids = patientIds.ToList();

        var visits = await db.Appointments
            .Where(a => ids.Contains(a.PatientId) && a.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.PatientId)
            .Select(g => new { PatientId = g.Key, First = g.Min(a => a.StartAt), Last = g.Max(a => a.StartAt), Count = g.Count() })
            .ToDictionaryAsync(x => x.PatientId, ct);

        var upcoming = await db.Appointments
            .Where(a => ids.Contains(a.PatientId) && Open.Contains(a.Status) && a.EndAt > now)
            .Select(a => new { a.Id, a.PatientId, a.StartAt, a.ProviderId })
            .ToListAsync(ct);
        var next = upcoming
            .GroupBy(a => a.PatientId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartAt).Select(a => new PatientNextAppointment(a.Id, a.StartAt, a.ProviderId)).First());

        var balances = await BalancesAsync(ids, ct);

        var documents = await db.PatientDocuments
            .Where(d => ids.Contains(d.PatientId))
            .GroupBy(d => d.PatientId)
            .Select(g => new { PatientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PatientId, x => x.Count, ct);

        var allergies = (await db.Allergies
                .Where(a => ids.Contains(a.PatientId))
                .Select(a => new { a.PatientId, a.Severity })
                .ToListAsync(ct))
            .GroupBy(a => a.PatientId)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Severest: g.Select(a => a.Severity).OrderBy(Rank).FirstOrDefault()));

        return ids.ToDictionary(id => id, id => new PatientInsights(
            visits.TryGetValue(id, out var visit) ? visit.Last : null,
            next.GetValueOrDefault(id),
            balances.GetValueOrDefault(id),
            allergies.TryGetValue(id, out var a) ? a.Count : 0,
            allergies.TryGetValue(id, out var b) ? b.Severest : null,
            visit?.Count ?? 0,
            documents.GetValueOrDefault(id),
            visit?.First));
    }

    private static int Rank(string? severity)
    {
        var i = Array.IndexOf(SeverityRank, severity);
        return i < 0 ? SeverityRank.Length : i;
    }

    /// <summary>What each patient owes on invoices that are not void.</summary>
    private async Task<Dictionary<Guid, decimal>> BalancesAsync(List<Guid> patientIds, CancellationToken ct)
    {
        var invoices = db.Invoices.Where(i => i.Status != InvoiceStatus.Void && patientIds.Contains(i.PatientId));

        var dues = await invoices
            .Select(i => new
            {
                i.PatientId,
                Billed = i.LineItems.Sum(l => l.Quantity * l.UnitFee),
                Paid = i.Payments.Sum(p => p.Amount),
            })
            .ToListAsync(ct);

        return dues.GroupBy(d => d.PatientId).ToDictionary(g => g.Key, g => g.Sum(d => d.Billed - d.Paid));
    }
}

/// <summary>Everything that happened to one patient, newest first (patients redesign).</summary>
internal sealed class PatientTimelineReader(ApplicationDbContext db) : IPatientTimelineReader
{
    public async Task<IReadOnlyList<PatientTimelineEntry>> GetAsync(Guid patientId, int limit, CancellationToken ct)
    {
        var appointments = await (
            from a in db.Appointments
            where a.PatientId == patientId
            join t in db.AppointmentTypes on a.AppointmentTypeId equals t.Id into types
            from t in types.DefaultIfEmpty()
            join s in db.StaffMembers on a.ProviderId equals s.Id into staff
            from s in staff.DefaultIfEmpty()
            orderby a.StartAt descending
            select new PatientTimelineEntry(a.StartAt, "appointment", a.Id, a.Status,
                t != null ? t.Name : null, s != null ? s.FirstName + " " + s.LastName : null, null))
            .Take(limit).ToListAsync(ct);

        var treatments = await (
            from i in db.TreatmentPlanItems
            join p in db.TreatmentPlans on i.TreatmentPlanId equals p.Id
            where p.PatientId == patientId
            orderby i.CreatedAt descending
            select new { At = i.UpdatedAt ?? i.CreatedAt, i.Id, i.Status, i.Description, i.ToothNumber, i.Fee })
            .Take(limit).ToListAsync(ct);

        var invoices = await db.Invoices
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => new { i.IssuedAt, i.Id, i.Status, i.InvoiceNumber, Total = i.LineItems.Sum(l => l.Quantity * l.UnitFee) })
            .Take(limit).ToListAsync(ct);

        var payments = await (
            from pay in db.InvoicePayments
            join i in db.Invoices on pay.InvoiceId equals i.Id
            where i.PatientId == patientId
            orderby pay.PaidAt descending
            select new { pay.PaidAt, pay.Id, pay.Method, i.InvoiceNumber, pay.Amount })
            .Take(limit).ToListAsync(ct);

        var documents = await db.PatientDocuments
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new PatientTimelineEntry(d.CreatedAt, "document", d.Id, d.Category, d.FileName, null, null))
            .Take(limit).ToListAsync(ct);

        var findings = await db.ToothFindings
            .Where(f => f.PatientId == patientId)
            .OrderByDescending(f => f.RecordedAt)
            .Select(f => new { f.RecordedAt, f.Id, f.Tooth, f.Condition, f.Surfaces })
            .Take(limit).ToListAsync(ct);

        return appointments
            .Concat(treatments.Select(t => new PatientTimelineEntry(t.At, "treatment", t.Id, t.Status.ToString(), t.Description,
                t.ToothNumber?.ToString(), t.Fee)))
            .Concat(invoices.Select(i => new PatientTimelineEntry(i.IssuedAt, "invoice", i.Id, i.Status.ToString(), i.InvoiceNumber, null, i.Total)))
            .Concat(payments.Select(p => new PatientTimelineEntry(p.PaidAt, "payment", p.Id, p.Method.ToString(), p.InvoiceNumber, null, p.Amount)))
            .Concat(documents)
            .Concat(findings.Select(f => new PatientTimelineEntry(f.RecordedAt, "finding", f.Id, null,
                f.Surfaces is null ? $"{f.Tooth} {f.Condition}" : $"{f.Tooth} {f.Condition} {f.Surfaces}", null, null)))
            .OrderByDescending(e => e.At)
            .Take(limit)
            .ToList();
    }
}
