using PearlDesk.Domain.Common;

namespace PearlDesk.Patients.Domain;

public class PatientEmergencyContact : TenantAuditableEntity
{
    public Guid PatientId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Relationship { get; private set; }
    public string? PhonePrimary { get; private set; }
    public string? PhoneSecondary { get; private set; }
    public string? Email { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation
    public Patient Patient { get; private set; } = default!;

    private PatientEmergencyContact() { }

    public static PatientEmergencyContact Create(
        Guid patientId,
        string name,
        string? relationship,
        string? phonePrimary,
        bool isPrimary = false)
    {
        return new PatientEmergencyContact
        {
            PatientId = patientId,
            Name = name,
            Relationship = relationship,
            PhonePrimary = phonePrimary,
            IsPrimary = isPrimary
        };
    }
}

