using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public class CreatePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<CreatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        CreatePatientCommand command,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var existing = await patientRepository.GetByEmailAsync(command.Email, cancellationToken);
            if (existing is not null)
                return PatientErrors.AlreadyExists;
        }

        var patientNumber = command.WithoutCard ? null
            : !string.IsNullOrWhiteSpace(command.PatientNumber) ? command.PatientNumber
            : await patientRepository.GeneratePatientNumberAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.PatientNumber))
        {
            var numberTaken = await patientRepository.GetByPatientNumberAsync(command.PatientNumber, cancellationToken);
            if (numberTaken is not null)
                return PatientErrors.PatientNumberAlreadyExists;
        }

        var patient = Patient.Create(
            patientNumber,
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.Email,
            command.PhoneMobile,
            command.Gender);

        patient.Update(
            command.FirstName,
            command.LastName,
            command.PreferredName,
            command.ParentName,
            command.DateOfBirth,
            command.Gender,
            null,
            command.Email,
            command.PhoneMobile,
            command.PhoneHome,
            command.PhoneWork,
            command.PreferredContactMethod,
            command.AddressLine1,
            command.AddressLine2,
            command.City,
            command.StateProvince,
            command.PostalCode,
            command.CountryCode,
            command.Occupation,
            command.PreferredProviderId,
            command.SmsOptIn,
            command.EmailOptIn,
            command.Notes);

        patient.SetFirstVisitDate(DateOnly.FromDateTime(DateTime.UtcNow));

        await patientRepository.AddAsync(patient, cancellationToken);

        return PatientResponse.FromEntity(patient);
    }
}

