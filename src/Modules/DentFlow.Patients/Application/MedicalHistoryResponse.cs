using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application;

public record MedicalHistoryResponse(
    Guid Id,
    string? BloodType,
    bool? IsPregnant,
    bool IsSmoker,
    bool IsDiabetic,
    bool HasHeartCondition,
    bool HasHypertension,
    bool HasBleedingDisorder,
    bool IsOnBloodThinners,
    bool HasPacemaker,
    bool HasArtificialJoints,
    bool HasLatexAllergy,
    string? GeneralNotes,
    string? CurrentMedications,
    string? PhysicianName,
    string? PhysicianPhone,
    DateTime RecordedAt)
{
    public static MedicalHistoryResponse FromEntity(MedicalHistory m) => new(
        m.Id,
        m.BloodType,
        m.IsPregnant,
        m.IsSmoker,
        m.IsDiabetic,
        m.HasHeartCondition,
        m.HasHypertension,
        m.HasBleedingDisorder,
        m.IsOnBloodThinners,
        m.HasPacemaker,
        m.HasArtificialJoints,
        m.HasLatexAllergy,
        m.GeneralNotes,
        m.CurrentMedications,
        m.PhysicianName,
        m.PhysicianPhone,
        m.RecordedAt);
}
