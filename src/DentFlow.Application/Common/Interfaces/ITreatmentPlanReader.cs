namespace DentFlow.Application.Common.Interfaces;

public record TreatmentPlanLineItem(
    Guid TreatmentPlanItemId,
    string Description,
    string? CdtCode,
    int? ToothNumber,
    decimal Fee);

/// <summary>
/// Cross-module reader so the Billing module can fetch treatment plan items
/// without directly referencing the Treatments module assembly.
/// </summary>
public interface ITreatmentPlanReader
{
    Task<(Guid PatientId, IReadOnlyList<TreatmentPlanLineItem> Items)?> GetPlanItemsAsync(
        Guid treatmentPlanId,
        CancellationToken cancellationToken = default);
}
