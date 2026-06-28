using ErrorOr;
using MediatR;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Commands;

public class UpsertMedicalHistoryCommandHandler(IPatientRepository repo)
    : IRequestHandler<UpsertMedicalHistoryCommand, ErrorOr<MedicalHistoryResponse>>
{
    public async Task<ErrorOr<MedicalHistoryResponse>> Handle(
        UpsertMedicalHistoryCommand cmd, CancellationToken ct)
    {
        var existing = await repo.GetCurrentMedicalHistoriesAsync(cmd.PatientId, ct);
        foreach (var old in existing)
            old.MarkAsNotCurrent();

        var record = MedicalHistory.Create(
            cmd.PatientId,
            recordedByStaffId: null,
            cmd.BloodType,
            cmd.IsPregnant,
            cmd.IsSmoker,
            cmd.IsDiabetic,
            cmd.HasHeartCondition,
            cmd.HasHypertension,
            cmd.GeneralNotes,
            cmd.CurrentMedications,
            cmd.PhysicianName,
            cmd.PhysicianPhone,
            cmd.HasBleedingDisorder,
            cmd.IsOnBloodThinners,
            cmd.HasPacemaker,
            cmd.HasArtificialJoints,
            cmd.HasLatexAllergy);

        await repo.AddMedicalHistoryAsync(record, ct);
        return MedicalHistoryResponse.FromEntity(record);
    }
}
