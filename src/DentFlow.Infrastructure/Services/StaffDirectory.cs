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

    public async Task<StaffBookability?> GetBookabilityAsync(Guid staffId, CancellationToken ct)
    {
        var staff = await db.Set<StaffMember>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffId, ct);
        return staff is null ? null : new StaffBookability(staff.IsActive, staff.IsBookable);
    }
}
