using DentFlow.Application.Common;

namespace DentFlow.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<DashboardFinanceResponse> GetFinanceAsync(CancellationToken cancellationToken = default);
}
