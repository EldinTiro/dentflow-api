using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

/// <summary>
/// People-merge plan D9: delete is for a patient created by mistake (a duplicate, a typo).
/// Anyone with clinical or financial history is deactivated instead, so nothing is hidden.
/// </summary>
public class DeletePatientCommandHandler(IPatientRepository patientRepository, IPatientHistoryReader history)
    : IRequestHandler<DeletePatientCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeletePatientCommand command,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(command.Id, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        if (await history.HasHistoryAsync(patient.Id, cancellationToken))
            return PatientErrors.HasHistory;

        await patientRepository.SoftDeleteAsync(patient, cancellationToken);

        return Result.Deleted;
    }
}
