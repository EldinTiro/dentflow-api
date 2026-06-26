using DentFlow.Notifications.Domain;

namespace DentFlow.Notifications.Application.Interfaces;

public interface ITenantNotificationConfigRepository
{
    Task<TenantNotificationConfig?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(TenantNotificationConfig config, CancellationToken ct);
    Task UpdateAsync(TenantNotificationConfig config, CancellationToken ct);
}
