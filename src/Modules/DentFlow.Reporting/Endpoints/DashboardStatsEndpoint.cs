using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace DentFlow.Reporting.Endpoints;

public class DashboardStatsEndpoint(IDashboardService dashboardService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/dashboard/stats");
        Policies(Permission.DashboardView);
        Version(1);
        Summary(s =>
        {
            s.Summary     = "Get dashboard KPI summary stats for the current tenant";
            s.Description = "Returns new patients, appointment completion trends and recall metrics. Money is on /dashboard/finance.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var stats = await dashboardService.GetStatsAsync(ct);
        await SendOkAsync(stats, ct);
    }
}
