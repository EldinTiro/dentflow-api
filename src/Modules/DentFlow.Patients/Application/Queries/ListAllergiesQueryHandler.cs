using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Queries;

public class ListAllergiesQueryHandler(IPatientRepository repo)
    : IRequestHandler<ListAllergiesQuery, ErrorOr<IReadOnlyList<AllergyResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<AllergyResponse>>> Handle(
        ListAllergiesQuery query, CancellationToken ct)
    {
        var items = await repo.ListAllergiesAsync(query.PatientId, ct);
        return items.Select(AllergyResponse.FromEntity).ToList().AsReadOnly();
    }
}
