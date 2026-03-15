using PearlDesk.Domain.Common;

namespace PearlDesk.Patients.Domain;

public class Allergy : TenantAuditableEntity
{
    public Guid PatientId { get; private set; }
    public string Allergen { get; private set; } = default!;
    public string? Reaction { get; private set; }
    public string? Severity { get; private set; }
    public string? Notes { get; private set; }
    public DateOnly? ReportedAt { get; private set; }

    // Navigation
    public Patient Patient { get; private set; } = default!;

    private Allergy() { }

    public static Allergy Create(
        Guid patientId,
        string allergen,
        string? reaction,
        string? severity,
        string? notes = null)
    {
        return new Allergy
        {
            PatientId = patientId,
            Allergen = allergen,
            Reaction = reaction,
            Severity = severity,
            Notes = notes,
            ReportedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }
}

