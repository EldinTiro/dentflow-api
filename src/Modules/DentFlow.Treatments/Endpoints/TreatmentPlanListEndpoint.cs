using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Treatments.Application;
using DentFlow.Treatments.Application.Queries;
using DentFlow.Treatments.Domain;
using DentFlow.Application.Common.Http;

namespace DentFlow.Treatments.Endpoints;

public class TreatmentPlanListEndpoint(ISender sender)
    : EndpointWithoutRequest<PagedResult<TreatmentPlanResponse>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/treatment-plans");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "List treatment plans for a patient");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var statusStr = Query<string?>("status", isRequired: false);
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        TreatmentPlanStatus? status = null;
        if (!string.IsNullOrWhiteSpace(statusStr) &&
            Enum.TryParse<TreatmentPlanStatus>(statusStr, ignoreCase: true, out var parsed))
            status = parsed;

        var query = new ListTreatmentPlansQuery(
            patientId,
            status,
            page > 0 ? page : 1,
            pageSize > 0 ? pageSize : 20);

        var result = await sender.Send(query, ct);

        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
