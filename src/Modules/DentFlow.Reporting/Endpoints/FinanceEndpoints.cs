using DentFlow.Application.Common.Http;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using FastEndpoints;

namespace DentFlow.Reporting.Endpoints;

/// <summary>
/// The owner's Finance section (documentation/roles-permissions-proposal-2026-10-22.md):
/// clinic-level money, so every endpoint requires finance.view.
/// </summary>
internal static class FinancePeriod
{
    /// <summary>`from`/`to` as the client's local-day instants; defaults to the last 30 days.</summary>
    public static (DateTime From, DateTime To) Read(string? from, string? to)
    {
        var now = DateTime.UtcNow;
        return (DateRangeBound.Parse(from, endOfDay: false) ?? now.Date.AddDays(-30),
                DateRangeBound.Parse(to, endOfDay: true) ?? now);
    }
}

public class FinancePaymentsEndpoint(IFinanceReportService reports) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/finance/payments");
        Policies(Permission.FinanceView);
        Version(1);
        Summary(s => s.Summary = "Payments taken in a period, with patient and invoice (daily cash-up, revenue)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (from, to) = FinancePeriod.Read(Query<string?>("from", isRequired: false), Query<string?>("to", isRequired: false));
        await SendOkAsync(await reports.GetPaymentsAsync(from, to, ct), ct);
    }
}

public class FinanceSummaryEndpoint(IFinanceReportService reports) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/finance/summary");
        Policies(Permission.FinanceView);
        Version(1);
        Summary(s => s.Summary = "Amount invoiced in a period, plus what is outstanding and overdue now");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var (from, to) = FinancePeriod.Read(Query<string?>("from", isRequired: false), Query<string?>("to", isRequired: false));
        await SendOkAsync(await reports.GetSummaryAsync(from, to, ct), ct);
    }
}

public class FinanceDebtorsEndpoint(IFinanceReportService reports) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/finance/debtors");
        Policies(Permission.FinanceView);
        Version(1);
        Summary(s => s.Summary = "Patients who owe money, oldest open invoice first");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await SendOkAsync(await reports.GetDebtorsAsync(ct), ct);
}
