namespace DentFlow.Application.Common;

/// <summary>Operational dashboard figures. Deliberately no money — see <see cref="DashboardFinanceResponse"/>.</summary>
public sealed record DashboardStatsResponse(
    int NewPatientsThisMonth,
    int AppointmentsCompletedThisWeek,
    int AppointmentsCompletedLastWeek,
    int RecallDueSoon,
    int RecallOverdue);
