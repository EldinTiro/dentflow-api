using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

/// <summary>
/// Opens the card of a patient saved without one: the paper card's number, or the clinic's
/// next number when none is given (client request 2026-11-10).
/// </summary>
public record OpenPatientCardCommand(Guid Id, string? PatientNumber) : IRequest<ErrorOr<PatientResponse>>;

public class OpenPatientCardCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<OpenPatientCardCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(OpenPatientCardCommand command, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(command.Id, cancellationToken);
        if (patient is null) return PatientErrors.NotFound;
        if (patient.HasCard) return PatientErrors.AlreadyHasCard;

        var requested = command.PatientNumber?.Trim();
        string number;
        if (string.IsNullOrEmpty(requested))
        {
            number = await patientRepository.GeneratePatientNumberAsync(cancellationToken);
        }
        else
        {
            if (await patientRepository.GetByPatientNumberAsync(requested, cancellationToken) is not null)
                return PatientErrors.PatientNumberAlreadyExists;
            number = requested;
        }

        var opened = patient.OpenCard(number);
        if (opened.IsError) return opened.Errors;

        await patientRepository.UpdateAsync(patient, cancellationToken);
        return PatientResponse.FromEntity(patient);
    }
}
