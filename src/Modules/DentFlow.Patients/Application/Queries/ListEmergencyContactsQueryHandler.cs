using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Queries;

public class ListEmergencyContactsQueryHandler(IPatientRepository repo)
    : IRequestHandler<ListEmergencyContactsQuery, ErrorOr<IReadOnlyList<PatientEmergencyContactResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<PatientEmergencyContactResponse>>> Handle(
        ListEmergencyContactsQuery query, CancellationToken ct)
    {
        var items = await repo.ListEmergencyContactsAsync(query.PatientId, ct);
        return items.Select(PatientEmergencyContactResponse.FromEntity).ToList().AsReadOnly();
    }
}
