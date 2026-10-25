using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Billing.Domain;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Patients.Domain;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Tenant scoping and soft deletes come from the global query filters (QA-003).
/// Balances reuse the Invoice domain rules so the numbers match the invoice list.
/// </summary>
internal sealed class FinanceReportService(ApplicationDbContext db) : IFinanceReportService
{
    public async Task<IReadOnlyList<FinancePaymentRow>> GetPaymentsAsync(DateTime start, DateTime end, CancellationToken ct = default)
    {
        var rows = await (
            from p in db.Set<InvoicePayment>()
            join i in db.Set<Invoice>() on p.InvoiceId equals i.Id
            join pat in db.Set<Patient>() on i.PatientId equals pat.Id
            where p.PaidAt >= start && p.PaidAt <= end
            orderby p.PaidAt
            select new
            {
                i.Id, i.InvoiceNumber, PatientId = pat.Id, pat.FirstName, pat.LastName,
                p.Amount, p.Method, p.PaidAt, p.Reference,
            }).ToListAsync(ct);

        return [.. rows.Select(r => new FinancePaymentRow(
            r.Id, r.InvoiceNumber, r.PatientId, $"{r.FirstName} {r.LastName}",
            r.Amount, r.Method.ToString(), r.PaidAt, r.Reference))];
    }

    public async Task<FinanceSummaryResponse> GetSummaryAsync(DateTime start, DateTime end, CancellationToken ct = default)
    {
        var invoiced = await (
            from li in db.Set<InvoiceLineItem>()
            join i in db.Set<Invoice>() on li.InvoiceId equals i.Id
            where i.Status != InvoiceStatus.Void && i.IssuedAt >= start && i.IssuedAt <= end
            select (decimal?)(li.Quantity * li.UnitFee)).SumAsync(ct) ?? 0m;

        var now = DateTime.UtcNow;
        var open = await OpenInvoicesAsync(ct);
        var overdue = open.Where(i => i.IsOverdueAt(now)).ToList();

        return new FinanceSummaryResponse(
            Invoiced: Math.Round(invoiced, 2),
            Outstanding: Math.Round(open.Sum(i => i.BalanceDue), 2),
            OutstandingPatients: open.Select(i => i.PatientId).Distinct().Count(),
            Overdue: Math.Round(overdue.Sum(i => i.BalanceDue), 2),
            OverduePatients: overdue.Select(i => i.PatientId).Distinct().Count());
    }

    public async Task<IReadOnlyList<FinanceDebtorRow>> GetDebtorsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var open = await OpenInvoicesAsync(ct);
        var patientIds = open.Select(i => i.PatientId).Distinct().ToList();

        var patients = await db.Set<Patient>()
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.PatientNumber, p.PhoneMobile })
            .ToDictionaryAsync(p => p.Id, ct);

        return [.. open
            .GroupBy(i => i.PatientId)
            .Where(g => patients.ContainsKey(g.Key))
            .Select(g =>
            {
                var p = patients[g.Key];
                return new FinanceDebtorRow(
                    g.Key,
                    $"{p.FirstName} {p.LastName}",
                    p.PatientNumber,
                    p.PhoneMobile,
                    Math.Round(g.Sum(i => i.BalanceDue), 2),
                    Math.Round(g.Where(i => i.IsOverdueAt(now)).Sum(i => i.BalanceDue), 2),
                    g.Count(),
                    g.Min(i => i.IssuedAt));
            })
            .OrderBy(d => d.OldestOpenIssuedAt)];
    }

    /// <summary>Invoices with money still owed: not paid, not void, positive balance.</summary>
    private async Task<List<Invoice>> OpenInvoicesAsync(CancellationToken ct)
    {
        var candidates = await db.Set<Invoice>()
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void)
            .AsNoTracking()
            .ToListAsync(ct);

        return [.. candidates.Where(i => i.BalanceDue > 0)];
    }
}
