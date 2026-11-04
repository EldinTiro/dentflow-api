using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Queries;

public class ListPatientsQueryHandler(IPatientRepository patientRepository, IPatientInsightsReader insights)
    : IRequestHandler<ListPatientsQuery, ErrorOr<PagedResult<PatientResponse>>>
{
    public async Task<ErrorOr<PagedResult<PatientResponse>>> Handle(
        ListPatientsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var (items, total) = await patientRepository.ListAsync(
            query.SearchTerm,
            query.Status,
            query.RecallFilter,
            query.Page,
            query.PageSize,
            cancellationToken);

        // What the desk needs per row: last visit, next appointment, balance, allergies
        var facts = await insights.GetAsync(items.Select(p => p.Id).ToList(), now, cancellationToken);
        var responses = items
            .Select(p => PatientResponse.FromEntity(p) with { Insights = facts.GetValueOrDefault(p.Id) ?? PatientInsights.None })
            .ToList();
        return new PagedResult<PatientResponse>(responses, total, query.Page, query.PageSize);
    }
}
