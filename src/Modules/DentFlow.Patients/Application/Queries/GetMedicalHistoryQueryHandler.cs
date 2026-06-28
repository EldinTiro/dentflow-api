using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Queries;

public class GetMedicalHistoryQueryHandler(IPatientRepository repo)
    : IRequestHandler<GetMedicalHistoryQuery, ErrorOr<MedicalHistoryResponse?>>
{
    public async Task<ErrorOr<MedicalHistoryResponse?>> Handle(
        GetMedicalHistoryQuery query, CancellationToken ct)
    {
        var record = await repo.GetCurrentMedicalHistoryAsync(query.PatientId, ct);
        return record is null ? (MedicalHistoryResponse?)null : MedicalHistoryResponse.FromEntity(record);
    }
}
