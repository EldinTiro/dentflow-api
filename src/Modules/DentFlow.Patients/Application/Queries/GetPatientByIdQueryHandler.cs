using ErrorOr;
using DentFlow.Application.Common.Interfaces;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Queries;

public class GetPatientByIdQueryHandler(IPatientRepository patientRepository, IPatientHistoryReader history)
    : IRequestHandler<GetPatientByIdQuery, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        GetPatientByIdQuery query,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(query.Id, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        return PatientResponse.FromEntity(patient) with
        {
            HasHistory = await history.HasHistoryAsync(patient.Id, cancellationToken),
        };
    }
}

