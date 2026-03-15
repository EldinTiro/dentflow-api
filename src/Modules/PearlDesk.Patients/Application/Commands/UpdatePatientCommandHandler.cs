using ErrorOr;
using MediatR;
using PearlDesk.Patients.Application.Interfaces;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Commands;

public class UpdatePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<UpdatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        UpdatePatientCommand command,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(command.Id, cancellationToken);
        if (patient is null)
            return PatientErrors.NotFound;

        patient.Update(
            command.FirstName,
            command.LastName,
            command.PreferredName,
            command.DateOfBirth,
            command.Gender,
            command.Pronouns,
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

        await patientRepository.UpdateAsync(patient, cancellationToken);

        return PatientResponse.FromEntity(patient);
    }
}

