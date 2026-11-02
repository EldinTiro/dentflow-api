using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Infrastructure.Services;

internal sealed class StaffDirectory(ApplicationDbContext db) : IStaffDirectory
{
    public async Task RenameLinkedProfileAsync(Guid userId, string firstName, string lastName, CancellationToken ct)
    {
        var staff = await db.Set<StaffMember>().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (staff is null) return;

        staff.Rename(firstName, lastName);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ChangeLinkedProfileEmailAsync(Guid userId, string email, CancellationToken ct)
    {
        var staff = await db.Set<StaffMember>().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (staff is null) return true;

        var lower = email.ToLower();
        var takenByAnother = await db.Set<StaffMember>()
            .AnyAsync(s => s.Id != staff.Id && s.Email != null && s.Email.ToLower() == lower, ct);
        if (takenByAnother) return false;

        staff.UseSignInEmail(email);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task CreateLinkedProfileAsync(
        Guid userId, Guid tenantId, string firstName, string lastName, string email, string role, CancellationToken ct)
    {
        // Explicit tenant: the platform admin acts without a resolved clinic. If a clinic *is*
        // resolved it wins when saving, so a different one would put the profile in the wrong
        // clinic — refuse instead of misplacing it.
        if (db.ResolvedTenantId is { } resolved && resolved != tenantId)
            throw new InvalidOperationException(
                $"Cannot create a staff profile for tenant {tenantId} while tenant {resolved} is resolved.");

        // Same rule as the backfill — reuse a same-email profile that has no account yet.
        var lower = email.ToLower();
        var existing = await db.Set<StaffMember>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted && s.UserId == null
                && s.Email != null && s.Email.ToLower() == lower, ct);

        if (existing is not null)
        {
            existing.AttachExistingAccount(userId);
        }
        else
        {
            var profile = StaffMember.Create(StaffMember.TypeForAccountRole(role), firstName, lastName, email, null, null);
            profile.SetTenant(tenantId);
            profile.AttachExistingAccount(userId);
            db.Set<StaffMember>().Add(profile);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<StaffBookability?> GetBookabilityAsync(Guid staffId, CancellationToken ct)
    {
        var staff = await db.Set<StaffMember>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffId, ct);
        return staff is null ? null : new StaffBookability(staff.IsActive, staff.IsBookable);
    }
}
