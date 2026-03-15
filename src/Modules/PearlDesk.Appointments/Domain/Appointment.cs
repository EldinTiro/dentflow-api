using PearlDesk.Domain.Common;

namespace PearlDesk.Appointments.Domain;

public class Appointment : TenantAuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid? OperatoryId { get; private set; }
    public Guid AppointmentTypeId { get; private set; }
    public string Status { get; private set; } = AppointmentStatus.Scheduled;
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public bool IsNewPatient { get; private set; }
    public string? ChiefComplaint { get; private set; }
    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? NoShowAt { get; private set; }
    public DateTime? ReminderSentAt { get; private set; }
    public Guid? RecurrenceId { get; private set; }
    public string Source { get; private set; } = AppointmentSource.Staff;
    public string? ColorHex { get; private set; }

    private Appointment() { }

    public static Appointment Create(
        Guid patientId,
        Guid providerId,
        Guid appointmentTypeId,
        DateTime startAt,
        DateTime endAt,
        string? chiefComplaint = null,
        string? notes = null,
        Guid? operatoryId = null,
        bool isNewPatient = false,
        string source = AppointmentSource.Staff,
        string? colorHex = null)
    {
        return new Appointment
        {
            PatientId = patientId,
            ProviderId = providerId,
            AppointmentTypeId = appointmentTypeId,
            StartAt = startAt,
            EndAt = endAt,
            DurationMinutes = (int)(endAt - startAt).TotalMinutes,
            ChiefComplaint = chiefComplaint,
            Notes = notes,
            OperatoryId = operatoryId,
            IsNewPatient = isNewPatient,
            Source = source,
            ColorHex = colorHex
        };
    }

    public void Confirm()
    {
        Status = AppointmentStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void CheckIn()
    {
        Status = AppointmentStatus.CheckedIn;
        CheckedInAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Complete()
    {
        Status = AppointmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Cancel(string? reason, Guid? cancelledByUserId)
    {
        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        CancelledByUserId = cancelledByUserId;
        SetUpdated();
    }

    public void MarkNoShow()
    {
        Status = AppointmentStatus.NoShow;
        NoShowAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Reschedule(DateTime newStartAt, DateTime newEndAt)
    {
        StartAt = newStartAt;
        EndAt = newEndAt;
        DurationMinutes = (int)(newEndAt - newStartAt).TotalMinutes;
        Status = AppointmentStatus.Scheduled;
        SetUpdated();
    }
}

