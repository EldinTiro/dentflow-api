namespace DentFlow.Application.Common.Interfaces;

/// <summary>The visit a patient has coming up next.</summary>
public record PatientNextAppointment(Guid Id, DateTime StartAt, Guid ProviderId);

/// <summary>
/// What the desk needs to know per patient in a list: when they were last in, what is
/// booked next, what they owe, whether they have allergies, when they first came, how often
/// they came and how many files they have (patients redesign).
/// </summary>
public record PatientInsights(
    DateTime? LastVisitAt,
    PatientNextAppointment? NextAppointment,
    decimal BalanceDue,
    int AllergyCount,
    string? SeverestAllergy,
    int VisitCount,
    int DocumentCount,
    DateTime? FirstVisitAt)
{
    public static readonly PatientInsights None = new(null, null, 0m, 0, null, 0, 0, null);
}

/// <summary>Reads across appointments, billing and the patient record (Infrastructure).</summary>
public interface IPatientInsightsReader
{
    Task<IReadOnlyDictionary<Guid, PatientInsights>> GetAsync(
        IReadOnlyCollection<Guid> patientIds, DateTime now, CancellationToken ct);
}

/// <summary>One thing that happened to a patient, for their timeline.</summary>
/// <param name="Kind">appointment, treatment, invoice, payment, document or finding.</param>
/// <param name="Label">What it was: the appointment type, the treatment, the invoice number, the file…</param>
/// <param name="Detail">Who or where: the doctor, the tooth, the payment method…</param>
public record PatientTimelineEntry(
    DateTime At,
    string Kind,
    Guid RefId,
    string? Status,
    string? Label,
    string? Detail,
    decimal? Amount);

public interface IPatientTimelineReader
{
    Task<IReadOnlyList<PatientTimelineEntry>> GetAsync(Guid patientId, int limit, CancellationToken ct);
}
