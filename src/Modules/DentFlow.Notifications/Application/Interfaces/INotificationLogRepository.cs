using DentFlow.Notifications.Domain;

namespace DentFlow.Notifications.Application.Interfaces;

public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog log, CancellationToken ct);
    Task<NotificationLog?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken ct);
    Task UpdateAsync(NotificationLog log, CancellationToken ct);

    /// <summary>
    /// Returns true if a reminder for this appointment+slot has already been sent (any non-failed status).
    /// </summary>
    Task<bool> ExistsAsync(Guid appointmentId, byte reminderSlot, CancellationToken ct);
}
