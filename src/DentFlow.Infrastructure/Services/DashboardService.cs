using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Domain;
using DentFlow.Billing.Domain;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Patients.Domain;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Tenant scoping and soft-delete exclusion come from the global query filters. This
/// used to bypass them and filter by a tenant id parsed from the slug, which never
/// parsed — every clinic's dashboard read zeros (QA-003).
/// </summary>
internal sealed class DashboardService(ApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var now   = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

        // Week boundaries (Mon–Sun)
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var thisWeekStart = today.AddDays(-daysSinceMonday);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var lastWeekEnd   = thisWeekStart;

        // Month boundary
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Recall boundaries (treat as UTC-midnight for DateOnly comparisons)
        var todayOnly       = DateOnly.FromDateTime(today);
        var thirtyDaysLater = todayOnly.AddDays(30);

        // ── Revenue this month ──────────────────────────────────────────────
        var revenueThisMonth = await db.Set<InvoicePayment>()
            .Where(p => p.PaidAt >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        // ── Outstanding balance (all non-void invoices) ─────────────────────
        // SubTotal - PaidAmount for each invoice
        var invoiceTotals = await db.Set<Invoice>()
            .Where(i => i.Status != InvoiceStatus.Void &&
                        i.Status != InvoiceStatus.Paid)
            .Select(i => new
            {
                SubTotal  = db.Set<InvoiceLineItem>()
                              .Where(li => li.InvoiceId == i.Id)
                              .Sum(li => (decimal?)(li.Quantity * li.UnitFee)) ?? 0m,
                PaidAmount = db.Set<InvoicePayment>()
                              .Where(p => p.InvoiceId == i.Id)
                              .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var outstandingBalance = invoiceTotals.Sum(x => x.SubTotal - x.PaidAmount);

        // ── New patients this month ─────────────────────────────────────────
        var newPatientsThisMonth = await db.Set<Patient>()
            .CountAsync(p => p.FirstVisitDate != null &&
                             p.FirstVisitDate >= DateOnly.FromDateTime(monthStart),
                        cancellationToken);

        // ── Appointments completed this week ────────────────────────────────
        var completedThisWeek = await db.Set<Appointment>()
            .CountAsync(a => a.Status == AppointmentStatus.Completed &&
                             a.StartAt >= thisWeekStart &&
                             a.StartAt < thisWeekStart.AddDays(7),
                        cancellationToken);

        // ── Appointments completed last week ────────────────────────────────
        var completedLastWeek = await db.Set<Appointment>()
            .CountAsync(a => a.Status == AppointmentStatus.Completed &&
                             a.StartAt >= lastWeekStart &&
                             a.StartAt < lastWeekEnd,
                        cancellationToken);

        // ── Recall due soon (next 30 days) ──────────────────────────────────
        var recallDueSoon = await db.Set<Patient>()
            .CountAsync(p => p.Status == PatientStatus.Active &&
                             p.RecallDueDate != null &&
                             p.RecallDueDate >= todayOnly &&
                             p.RecallDueDate <= thirtyDaysLater,
                        cancellationToken);

        // ── Recall overdue ──────────────────────────────────────────────────
        var recallOverdue = await db.Set<Patient>()
            .CountAsync(p => p.Status == PatientStatus.Active &&
                             p.RecallDueDate != null &&
                             p.RecallDueDate < todayOnly,
                        cancellationToken);

        return new DashboardStatsResponse(
            RevenueThisMonth:               Math.Round(revenueThisMonth, 2),
            OutstandingBalance:             Math.Round(outstandingBalance, 2),
            NewPatientsThisMonth:           newPatientsThisMonth,
            AppointmentsCompletedThisWeek:  completedThisWeek,
            AppointmentsCompletedLastWeek:  completedLastWeek,
            RecallDueSoon:                  recallDueSoon,
            RecallOverdue:                  recallOverdue);
    }
}
