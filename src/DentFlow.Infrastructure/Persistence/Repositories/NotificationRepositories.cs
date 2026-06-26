using Microsoft.EntityFrameworkCore;
using DentFlow.Notifications.Application.Interfaces;
using DentFlow.Notifications.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Persistence.Repositories;

internal sealed class NotificationLogRepository(ApplicationDbContext db) : INotificationLogRepository
{
    public async Task AddAsync(NotificationLog log, CancellationToken ct)
    {
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task<NotificationLog?> GetByProviderMessageIdAsync(string providerMessageId, CancellationToken ct) =>
        await db.NotificationLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.ProviderMessageId == providerMessageId, ct);

    public async Task UpdateAsync(NotificationLog log, CancellationToken ct)
    {
        db.NotificationLogs.Update(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid appointmentId, byte reminderSlot, CancellationToken ct) =>
        await db.NotificationLogs
            .AnyAsync(n => n.AppointmentId == appointmentId &&
                           n.ReminderSlot == reminderSlot &&
                           n.Status != NotificationStatus.Failed, ct);
}

internal sealed class TenantNotificationConfigRepository(ApplicationDbContext db)
    : ITenantNotificationConfigRepository
{
    public async Task<TenantNotificationConfig?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct) =>
        await db.TenantNotificationConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

    public async Task AddAsync(TenantNotificationConfig config, CancellationToken ct)
    {
        db.TenantNotificationConfigs.Add(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TenantNotificationConfig config, CancellationToken ct)
    {
        db.TenantNotificationConfigs.Update(config);
        await db.SaveChangesAsync(ct);
    }
}
