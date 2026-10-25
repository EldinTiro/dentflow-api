using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Infrastructure;

public record StaffBackfillSummary(int Linked, int Created, int Skipped);

/// <summary>
/// People-merge plan 3.1: every clinic account gets exactly one staff profile. Runs at
/// startup after the role sync and is idempotent — linked accounts are left alone.
/// An account is linked to an unlinked same-clinic profile with its email (case-insensitive);
/// with no such profile one is created; with more than one, it is skipped and logged.
/// </summary>
public static class StaffAccountBackfill
{
    public static async Task<StaffBackfillSummary> RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(StaffAccountBackfill));

        var rows = await (
            from u in db.Users.AsNoTracking()
            where u.TenantId != null
            join ur in db.UserRoles on u.Id equals ur.UserId into urs
            from ur in urs.DefaultIfEmpty()
            join r in db.Roles on ur.RoleId equals r.Id into rs
            from r in rs.DefaultIfEmpty()
            select new { u.Id, TenantId = u.TenantId!.Value, u.Email, u.FirstName, u.LastName, u.IsActive, Role = r != null ? r.Name : null })
            .ToListAsync(ct);
        var accounts = rows.GroupBy(x => x.Id).Select(g => g.First()).ToList();

        // Not the tenant query filter: this runs outside any request and spans every clinic.
        var staff = db.Set<StaffMember>().IgnoreQueryFilters().Where(s => !s.IsDeleted);
        var alreadyLinked = (await staff.Where(s => s.UserId != null).Select(s => s.UserId!.Value).ToListAsync(ct)).ToHashSet();

        int linked = 0, created = 0, skipped = 0;
        foreach (var account in accounts)
        {
            if (alreadyLinked.Contains(account.Id) || account.Role == Roles.SuperAdmin || string.IsNullOrWhiteSpace(account.Email))
                continue;

            var email = account.Email.ToLower();
            var candidates = await staff
                .Where(s => s.TenantId == account.TenantId && s.UserId == null && s.Email != null && s.Email.ToLower() == email)
                .ToListAsync(ct);

            if (candidates.Count > 1)
            {
                skipped++;
                logger.LogWarning("Staff backfill skipped account {UserId}: {Count} profiles share its email in tenant {TenantId}",
                    account.Id, candidates.Count, account.TenantId);
                continue;
            }

            if (candidates.Count == 1)
            {
                candidates[0].AttachExistingAccount(account.Id);
                linked++;
                continue;
            }

            var profile = StaffMember.Create(TypeFor(account.Role), account.FirstName, account.LastName, account.Email, null, null);
            profile.SetTenant(account.TenantId);
            profile.AttachExistingAccount(account.Id);
            if (!account.IsActive) profile.Deactivate();
            db.Set<StaffMember>().Add(profile);
            created++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Staff backfill: {Linked} linked, {Created} created, {Skipped} skipped", linked, created, skipped);
        return new StaffBackfillSummary(linked, created, skipped);
    }

    /// <summary>Owners who don't treat patients get a non-bookable profile (D3).</summary>
    private static StaffType TypeFor(string? role) => role switch
    {
        Roles.Dentist => StaffType.Dentist,
        Roles.DentalNurse => StaffType.DentalAssistant,
        Roles.Receptionist => StaffType.Receptionist,
        _ => StaffType.OfficeManager,
    };
}
