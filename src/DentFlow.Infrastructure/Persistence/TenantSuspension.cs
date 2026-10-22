using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Persistence;

/// <summary>
/// A deactivated clinic is suspended: its users can neither sign in, refresh, nor use
/// a token issued before the deactivation (QA-002). Checked against the database on
/// each use so reactivation takes effect immediately.
/// </summary>
public static class TenantSuspension
{
    public static Task<bool> IsTenantSuspendedAsync(this ApplicationDbContext db, Guid? tenantId, CancellationToken ct) =>
        tenantId is { } id
            ? db.Tenants.AsNoTracking().AnyAsync(t => t.Id == id && !t.IsActive, ct)
            : Task.FromResult(false);
}
