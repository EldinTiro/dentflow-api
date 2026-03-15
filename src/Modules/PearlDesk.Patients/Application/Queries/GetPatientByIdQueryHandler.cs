using ErrorOr;
using MediatR;
using PearlDesk.Patients.Application.Interfaces;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Queries;

public class GetPatientByIdQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetPatientByIdQuery, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        GetPatientByIdQuery query,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(query.Id, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        return PatientResponse.FromEntity(patient);
    }
}

