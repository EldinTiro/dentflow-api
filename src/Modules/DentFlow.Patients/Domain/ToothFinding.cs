using ErrorOr;
using DentFlow.Domain.Clinical;
using DentFlow.Domain.Common;

namespace DentFlow.Patients.Domain;

/// <summary>What a tooth chart can say about a tooth.</summary>
public static class ToothCondition
{
    public const string Caries = "Caries";
    public const string Filling = "Filling";
    public const string Crown = "Crown";
    public const string RootCanal = "RootCanal";
    public const string Implant = "Implant";
    public const string Missing = "Missing";

    public static readonly IReadOnlyList<string> All = [Caries, Filling, Crown, RootCanal, Implant, Missing];

    /// <summary>Only these sit on particular surfaces; the rest concern the whole tooth.</summary>
    public static bool HasSurfaces(string condition) => condition is Caries or Filling;
}

/// <summary>
/// One finding on one tooth of a patient's chart (FDI numbering). Soft-deleted like the rest
/// of the clinical record, so a removed finding stays recoverable.
/// </summary>
public class ToothFinding : TenantAuditableEntity
{
    /// <summary>Mesial, occlusal/incisal, distal, buccal/labial, lingual/palatal — in that order.</summary>
    private const string SurfaceOrder = "MODBL";

    public Guid PatientId { get; private set; }
    public int Tooth { get; private set; }
    public string Condition { get; private set; } = default!;
    public string? Surfaces { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private ToothFinding() { }

    public static ErrorOr<ToothFinding> Create(Guid patientId, int tooth, string condition, string? surfaces, string? notes)
    {
        if (!Fdi.IsPermanentTooth(tooth))
            return ToothFindingErrors.InvalidTooth;
        if (!ToothCondition.All.Contains(condition))
            return ToothFindingErrors.InvalidCondition;

        var given = (surfaces ?? string.Empty).Trim().ToUpperInvariant();
        if (given.Length > 0 && !ToothCondition.HasSurfaces(condition))
            return ToothFindingErrors.SurfacesNotApplicable;
        if (given.Any(c => !SurfaceOrder.Contains(c)) || given.Distinct().Count() != given.Length)
            return ToothFindingErrors.InvalidSurfaces;

        return new ToothFinding
        {
            PatientId = patientId,
            Tooth = tooth,
            Condition = condition,
            Surfaces = given.Length == 0 ? null : string.Concat(SurfaceOrder.Where(given.Contains)),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedAt = DateTime.UtcNow,
        };
    }
}

public static class ToothFindingErrors
{
    public static readonly Error InvalidTooth =
        Error.Validation("ToothFinding.InvalidTooth", "Use an FDI tooth number: 11–18, 21–28, 31–38 or 41–48.");
    public static readonly Error InvalidCondition =
        Error.Validation("ToothFinding.InvalidCondition", "Unknown tooth condition.");
    public static readonly Error InvalidSurfaces =
        Error.Validation("ToothFinding.InvalidSurfaces", "Surfaces are M, O, D, B and L, each at most once.");
    public static readonly Error SurfacesNotApplicable =
        Error.Validation("ToothFinding.SurfacesNotApplicable", "Only caries and fillings sit on particular surfaces.");
    public static readonly Error NotFound =
        Error.NotFound("ToothFinding.NotFound", "Tooth finding not found.");
}
