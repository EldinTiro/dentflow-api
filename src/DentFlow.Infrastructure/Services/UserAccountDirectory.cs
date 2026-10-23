using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

internal sealed class UserAccountDirectory(ApplicationDbContext db) : IUserAccountDirectory
{
    public async Task<IReadOnlyDictionary<Guid, StaffAccessSummary>> GetSummariesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, StaffAccessSummary>();
        var ids = userIds.Distinct().ToList();

        // Users with their (single) role in one round trip.
        var rows = await (
            from u in db.Users.AsNoTracking()
            where ids.Contains(u.Id)
            join ur in db.UserRoles on u.Id equals ur.UserId into urs
            from ur in urs.DefaultIfEmpty()
            join r in db.Roles on ur.RoleId equals r.Id into rs
            from r in rs.DefaultIfEmpty()
            select new { u.Id, Role = r != null ? r.Name : null, u.IsActive, u.MustChangePassword, u.LastLoginAt })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.Id)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new StaffAccessSummary(x.Id, x.Role, x.IsActive, x.MustChangePassword, x.LastLoginAt)).First());
    }
}
