using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Application;

public record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid ProviderId,
    Guid? OperatoryId,
    Guid AppointmentTypeId,
    string Status,
    DateTime StartAt,
    DateTime EndAt,
    int DurationMinutes,
    bool IsNewPatient,
    string? ChiefComplaint,
    string? Notes,
    string? CancellationReason,
    DateTime? CancelledAt,
    DateTime? ConfirmedAt,
    DateTime? CheckedInAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string Source,
    string? ColorHex,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? PatientName = null)
{
    public static AppointmentResponse FromEntity(Appointment a) => FromEntity(a, null);

    public static AppointmentResponse FromEntity(Appointment a, string? patientName) => new(
        a.Id, a.PatientId, a.ProviderId, a.OperatoryId, a.AppointmentTypeId,
        a.Status, a.StartAt, a.EndAt, a.DurationMinutes, a.IsNewPatient,
        a.ChiefComplaint, a.Notes, a.CancellationReason, a.CancelledAt,
        a.ConfirmedAt, a.CheckedInAt, a.StartedAt, a.CompletedAt,
        a.Source, a.ColorHex, a.CreatedAt, a.UpdatedAt, patientName);
}

public record AppointmentStatusHistoryResponse(
    Guid Id,
    Guid AppointmentId,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    string? Reason,
    bool IsOverride,
    DateTime ChangedAt)
{
    public static AppointmentStatusHistoryResponse FromEntity(AppointmentStatusHistory h) => new(
        h.Id, h.AppointmentId, h.FromStatus, h.ToStatus,
        h.ChangedByUserId, h.Reason, h.IsOverride, h.ChangedAt);
}

public record AppointmentTypeResponse(
    Guid Id,
    string Name,
    string? Description,
    int DefaultDurationMinutes,
    string? ColorHex,
    bool IsBookableOnline,
    bool RequiresNewPatientForm,
    int SortOrder,
    decimal? DefaultFee)
{
    public static AppointmentTypeResponse FromEntity(AppointmentType at) => new(
        at.Id, at.Name, at.Description, at.DefaultDurationMinutes,
        at.ColorHex, at.IsBookableOnline, at.RequiresNewPatientForm, at.SortOrder, at.DefaultFee);
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

