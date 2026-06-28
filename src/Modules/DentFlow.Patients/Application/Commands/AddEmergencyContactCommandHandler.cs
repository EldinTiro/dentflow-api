using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public class AddEmergencyContactCommandHandler(IPatientRepository repo)
    : IRequestHandler<AddEmergencyContactCommand, ErrorOr<PatientEmergencyContactResponse>>
{
    public async Task<ErrorOr<PatientEmergencyContactResponse>> Handle(
        AddEmergencyContactCommand cmd, CancellationToken ct)
    {
        var contact = PatientEmergencyContact.Create(
            cmd.PatientId, cmd.Name, cmd.Relationship, cmd.PhonePrimary, cmd.IsPrimary);

        await repo.AddEmergencyContactAsync(contact, ct);
        return PatientEmergencyContactResponse.FromEntity(contact);
    }
}
