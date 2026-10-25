namespace DentFlow.Application.Common;

/// <summary>One payment, with what it paid for — the rows of the daily cash-up and revenue charts.</summary>
public sealed record FinancePaymentRow(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid PatientId,
    string PatientName,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? Reference);

/// <summary>
/// What was invoiced in a period, and what is owed right now. Outstanding and overdue use
/// the same rules as the invoice list (<c>Invoice.BalanceDue</c>, <c>Invoice.IsOverdueAt</c>).
/// </summary>
public sealed record FinanceSummaryResponse(
    decimal Invoiced,
    decimal Outstanding,
    int OutstandingPatients,
    decimal Overdue,
    int OverduePatients);

/// <summary>A patient who owes money, for the Debtors list.</summary>
public sealed record FinanceDebtorRow(
    Guid PatientId,
    string PatientName,
    string PatientNumber,
    string? PhoneMobile,
    decimal Balance,
    decimal OverdueBalance,
    int OpenInvoices,
    DateTime OldestOpenIssuedAt);
