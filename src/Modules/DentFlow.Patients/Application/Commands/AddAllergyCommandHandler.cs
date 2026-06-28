using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public class AddAllergyCommandHandler(IPatientRepository repo)
    : IRequestHandler<AddAllergyCommand, ErrorOr<AllergyResponse>>
{
    public async Task<ErrorOr<AllergyResponse>> Handle(
        AddAllergyCommand cmd, CancellationToken ct)
    {
        var allergy = Allergy.Create(cmd.PatientId, cmd.Allergen, cmd.Reaction, cmd.Severity, cmd.Notes);
        await repo.AddAllergyAsync(allergy, ct);
        return AllergyResponse.FromEntity(allergy);
    }
}
