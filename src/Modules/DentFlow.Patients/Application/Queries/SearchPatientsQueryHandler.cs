using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Queries;

public class SearchPatientsQueryHandler(IPatientRepository repo)
    : IRequestHandler<SearchPatientsQuery, ErrorOr<IReadOnlyList<PatientSearchResult>>>
{
    public async Task<ErrorOr<IReadOnlyList<PatientSearchResult>>> Handle(
        SearchPatientsQuery query, CancellationToken ct)
    {
        var limit = Math.Min(query.Limit, 50);
        var (items, _) = await repo.ListAsync(query.Q, PatientStatus.Active, null, 1, limit, ct);

        IReadOnlyList<PatientSearchResult> results = items
            .Select(p => new PatientSearchResult(p.Id, p.PatientNumber, p.FullName, p.PhoneMobile, p.Email))
            .ToList()
            .AsReadOnly();

        return ErrorOrFactory.From(results);
    }
}
