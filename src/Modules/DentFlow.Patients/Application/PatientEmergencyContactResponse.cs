using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application;

public record PatientEmergencyContactResponse(
    Guid Id,
    string Name,
    string? Relationship,
    string? PhonePrimary,
    bool IsPrimary)
{
    public static PatientEmergencyContactResponse FromEntity(PatientEmergencyContact c) => new(
        c.Id,
        c.Name,
        c.Relationship,
        c.PhonePrimary,
        c.IsPrimary);
}
