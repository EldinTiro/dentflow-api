using ErrorOr;
using MediatR;
using PearlDesk.Patients.Application.Interfaces;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Commands;

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

        var patientNumber = await patientRepository.GeneratePatientNumberAsync(cancellationToken);

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

        await patientRepository.AddAsync(patient, cancellationToken);

        return PatientResponse.FromEntity(patient);
    }
}

