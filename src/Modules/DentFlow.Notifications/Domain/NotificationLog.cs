using DentFlow.Domain.Common;

namespace DentFlow.Notifications.Domain;

public class NotificationLog : TenantAuditableEntity
{
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public byte ReminderSlot { get; private set; }
    public string ToPhoneNumber { get; private set; } = default!;
    public string MessageBody { get; private set; } = default!;
    public NotificationStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(
        Guid appointmentId,
        Guid patientId,
        NotificationChannel channel,
        byte reminderSlot,
        string toPhoneNumber,
        string messageBody)
    {
        return new NotificationLog
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            Channel = channel,
            ReminderSlot = reminderSlot,
            ToPhoneNumber = toPhoneNumber,
            MessageBody = messageBody,
            Status = NotificationStatus.Queued
        };
    }

    public void MarkSent(string providerMessageId)
    {
        Status = NotificationStatus.Sent;
        ProviderMessageId = providerMessageId;
        SetUpdated();
    }

    public void MarkFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
        SetUpdated();
    }

    public void MarkDelivered()
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        SetUpdated();
    }
}
