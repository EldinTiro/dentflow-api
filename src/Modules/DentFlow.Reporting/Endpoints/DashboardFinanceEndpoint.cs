using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using FastEndpoints;

namespace DentFlow.Reporting.Endpoints;

/// <summary>
/// Clinic-level money for the dashboard. Split out of /dashboard/stats, which used to send
/// revenue to every clinic role (documentation/roles-permissions-proposal-2026-10-22.md, A1).
/// </summary>
public class DashboardFinanceEndpoint(IDashboardService dashboardService)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/dashboard/finance");
        Policies(Permission.FinanceView);
        Version(1);
        Summary(s =>
        {
            s.Summary     = "Get the clinic's revenue this month and outstanding balance";
            s.Description = "Requires the finance.view permission (owner, manager, accountants).";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var finance = await dashboardService.GetFinanceAsync(ct);
        await SendOkAsync(finance, ct);
    }
}
