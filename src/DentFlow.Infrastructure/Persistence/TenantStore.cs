using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Persistence;

/// <summary>
/// Resolves tenants from the <c>tenants</c> table — the same rows SuperAdmin creates.
///
/// This replaces Finbuckle's ConfigurationStore, which could only ever know about
/// tenants listed in appsettings. A clinic created at runtime was invisible to it,
/// so resolution silently failed and every request fell back to Guid.Empty.
///
/// <see cref="TenantInfo.Id"/> carries the tenant's Guid and <see cref="TenantInfo.Identifier"/>
/// its slug, so <c>TryGetByIdentifierAsync</c> accepts either: the claim strategy
/// supplies the Guid from the tid claim, the host strategy supplies the subdomain slug.
/// </summary>
public sealed class TenantStore(ApplicationDbContext db) : IMultiTenantStore<TenantInfo>
{
    public async Task<TenantInfo?> TryGetAsync(string id) =>
        Guid.TryParse(id, out var guid) ? await FindByIdAsync(guid) : null;

    public async Task<TenantInfo?> TryGetByIdentifierAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return null;

        // The tid claim holds a Guid; a subdomain holds a slug.
        if (Guid.TryParse(identifier, out var guid))
            return await FindByIdAsync(guid);

        var slug = identifier.ToLowerInvariant();
        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.Slug == slug)
            .Select(t => new TenantInfo { Id = t.Id.ToString(), Identifier = t.Slug, Name = t.Name })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TenantInfo>> GetAllAsync() =>
        await Query().ToListAsync();

    public async Task<IEnumerable<TenantInfo>> GetAllAsync(int take, int skip) =>
        await Query().OrderBy(t => t.Identifier).Skip(skip).Take(take).ToListAsync();

    private IQueryable<TenantInfo> Query() =>
        db.Tenants
            .AsNoTracking()
            .Select(t => new TenantInfo { Id = t.Id.ToString(), Identifier = t.Slug, Name = t.Name });

    private Task<TenantInfo?> FindByIdAsync(Guid id) =>
        db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TenantInfo { Id = t.Id.ToString(), Identifier = t.Slug, Name = t.Name })
            .FirstOrDefaultAsync();

    // Tenants are created through the Tenants module, not through Finbuckle.
    public Task<bool> TryAddAsync(TenantInfo tenantInfo) => Task.FromResult(false);
    public Task<bool> TryUpdateAsync(TenantInfo tenantInfo) => Task.FromResult(false);
    public Task<bool> TryRemoveAsync(string identifier) => Task.FromResult(false);
}
