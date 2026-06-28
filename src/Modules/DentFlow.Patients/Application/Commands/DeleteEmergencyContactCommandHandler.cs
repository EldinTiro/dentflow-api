using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Commands;

public class DeleteEmergencyContactCommandHandler(IPatientRepository repo)
    : IRequestHandler<DeleteEmergencyContactCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteEmergencyContactCommand cmd, CancellationToken ct)
    {
        var contact = await repo.GetEmergencyContactAsync(cmd.PatientId, cmd.ContactId, ct);
        if (contact is null) return Error.NotFound();

        await repo.DeleteEmergencyContactAsync(contact, ct);
        return Result.Deleted;
    }
}
