namespace PearlDesk.Domain.Common;

/// <summary>
/// Base class for all tenant-scoped entities.
/// Provides TenantId, audit timestamps, and soft delete.
/// EF Core Global Query Filters handle tenant isolation and soft delete automatically.
/// </summary>
public abstract class TenantAuditableEntity : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void SetTenant(Guid tenantId) => TenantId = tenantId;

    public void SetUpdated() => UpdatedAt = DateTime.UtcNow;

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}

