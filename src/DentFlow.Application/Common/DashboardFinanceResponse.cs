namespace DentFlow.Application.Common;

/// <summary>Clinic-level money on the dashboard; served only to holders of <c>finance.view</c>.</summary>
public sealed record DashboardFinanceResponse(
    decimal RevenueThisMonth,
    decimal OutstandingBalance);
