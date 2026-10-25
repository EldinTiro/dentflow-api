namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Clinic-level finance reports for the owner's Finance section. Periods are UTC instants
/// the client computed from the clinic's local days (see <c>DateRangeBound</c>, QA-008).
/// </summary>
public interface IFinanceReportService
{
    Task<IReadOnlyList<FinancePaymentRow>> GetPaymentsAsync(DateTime start, DateTime end, CancellationToken ct = default);
    Task<FinanceSummaryResponse> GetSummaryAsync(DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<FinanceDebtorRow>> GetDebtorsAsync(CancellationToken ct = default);
}
