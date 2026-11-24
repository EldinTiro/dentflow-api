using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

/// <summary>
/// People-merge plan D9: delete is for a patient created by mistake (a duplicate, a typo).
/// Anyone with clinical or financial history is deactivated instead, so nothing is hidden.
/// A patient whose card was never opened and who never came is deleted with the missed
/// visits (client request 2026-11-10).
/// </summary>
public class DeletePatientCommandHandler(
    IPatientRepository patientRepository, IPatientHistoryReader history, ITransactionRunner transaction)
    : IRequestHandler<DeletePatientCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeletePatientCommand command,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(command.Id, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        if (await history.HasHistoryAsync(patient.Id, missedVisitsAreHistory: patient.HasCard, cancellationToken))
            return PatientErrors.HasHistory;

        return await transaction.ExecuteAsync<Deleted>(async ct =>
        {
            if (!patient.HasCard) await history.RemoveMissedVisitsAsync(patient.Id, ct);
            await patientRepository.SoftDeleteAsync(patient, ct);
            return Result.Deleted;
        }, cancellationToken);
    }
}
