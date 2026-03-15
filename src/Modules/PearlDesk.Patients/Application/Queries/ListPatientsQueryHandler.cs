using ErrorOr;
using MediatR;
using PearlDesk.Patients.Application.Interfaces;

namespace PearlDesk.Patients.Application.Queries;

public class ListPatientsQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<ListPatientsQuery, ErrorOr<PagedResult<PatientResponse>>>
{
    public async Task<ErrorOr<PagedResult<PatientResponse>>> Handle(
        ListPatientsQuery query,
        CancellationToken cancellationToken)
    {
        var (items, total) = await patientRepository.ListAsync(
            query.SearchTerm,
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

        var responses = items.Select(PatientResponse.FromEntity).ToList();
        return new PagedResult<PatientResponse>(responses, total, query.Page, query.PageSize);
    }
}

