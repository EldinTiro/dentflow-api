using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application;

public record AllergyResponse(
    Guid Id,
    string Allergen,
    string? Reaction,
    string? Severity,
    string? Notes,
    DateOnly? ReportedAt)
{
    public static AllergyResponse FromEntity(Allergy a) => new(
        a.Id,
        a.Allergen,
        a.Reaction,
        a.Severity,
        a.Notes,
        a.ReportedAt);
}
