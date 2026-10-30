using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application;

public record ToothFindingResponse(
    Guid Id,
    int Tooth,
    string Condition,
    string? Surfaces,
    string? Notes,
    DateTime RecordedAt)
{
    public static ToothFindingResponse FromEntity(ToothFinding f) => new(
        f.Id, f.Tooth, f.Condition, f.Surfaces, f.Notes, f.RecordedAt);
}
