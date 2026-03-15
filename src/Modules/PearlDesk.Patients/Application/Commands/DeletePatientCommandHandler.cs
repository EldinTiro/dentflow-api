using ErrorOr;
using MediatR;
using PearlDesk.Patients.Application.Interfaces;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Commands;

public class DeletePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<DeletePatientCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeletePatientCommand command,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(command.Id, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        await patientRepository.SoftDeleteAsync(patient, cancellationToken);

        return Result.Deleted;
    }
}

