using PearlDesk.Domain.Common;

namespace PearlDesk.Patients.Domain;

public class MedicalHistory : TenantAuditableEntity
{
    public Guid PatientId { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid? RecordedByStaffId { get; private set; }
    public bool IsCurrent { get; private set; }
    public string? BloodType { get; private set; }
    public bool? IsPregnant { get; private set; }
    public bool IsSmoker { get; private set; }
    public bool IsDiabetic { get; private set; }
    public bool HasHeartCondition { get; private set; }
    public bool HasHypertension { get; private set; }
    public bool HasBleedingDisorder { get; private set; }
    public bool IsOnBloodThinners { get; private set; }
    public bool HasPacemaker { get; private set; }
    public bool HasArtificialJoints { get; private set; }
    public bool HasLatexAllergy { get; private set; }
    public string? GeneralNotes { get; private set; }
    public string? CurrentMedications { get; private set; }
    public string? PhysicianName { get; private set; }
    public string? PhysicianPhone { get; private set; }

    // Navigation
    public Patient Patient { get; private set; } = default!;

    private MedicalHistory() { }

    public static MedicalHistory Create(
        Guid patientId,
        Guid? recordedByStaffId,
        string? bloodType,
        bool? isPregnant,
        bool isSmoker,
        bool isDiabetic,
        bool hasHeartCondition,
        bool hasHypertension,
        string? generalNotes,
        string? currentMedications,
        string? physicianName,
        string? physicianPhone)
    {
        return new MedicalHistory
        {
            PatientId = patientId,
            RecordedByStaffId = recordedByStaffId,
            RecordedAt = DateTime.UtcNow,
            IsCurrent = true,
            BloodType = bloodType,
            IsPregnant = isPregnant,
            IsSmoker = isSmoker,
            IsDiabetic = isDiabetic,
            HasHeartCondition = hasHeartCondition,
            HasHypertension = hasHypertension,
            GeneralNotes = generalNotes,
            CurrentMedications = currentMedications,
            PhysicianName = physicianName,
            PhysicianPhone = physicianPhone
        };
    }

    public void MarkAsNotCurrent() => IsCurrent = false;
}

