using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;

namespace DentFlow.Patients.Application.Commands;

public class DeleteAllergyCommandHandler(IPatientRepository repo)
    : IRequestHandler<DeleteAllergyCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteAllergyCommand cmd, CancellationToken ct)
    {
        var allergy = await repo.GetAllergyAsync(cmd.PatientId, cmd.AllergyId, ct);
        if (allergy is null) return Error.NotFound();

        await repo.DeleteAllergyAsync(allergy, ct);
        return Result.Deleted;
    }
}
