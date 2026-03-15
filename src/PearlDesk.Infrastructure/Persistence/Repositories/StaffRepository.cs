﻿using Microsoft.EntityFrameworkCore;
using PearlDesk.Infrastructure.Persistence;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Infrastructure.Persistence.Repositories;

public class StaffRepository(ApplicationDbContext dbContext) : IStaffRepository
{
    public async Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<StaffMember>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<StaffMember?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await dbContext.Set<StaffMember>().FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
    }

    public async Task<(IReadOnlyList<StaffMember> Items, int Total)> ListAsync(
        StaffType? staffType, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<StaffMember>().AsQueryable();
        if (staffType.HasValue) query = query.Where(s => s.StaffType == staffType.Value);
        if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<StaffMember>().AddAsync(staffMember, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StaffMember staffMember, CancellationToken cancellationToken = default)
    {
        dbContext.Set<StaffMember>().Update(staffMember);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(StaffMember staffMember, CancellationToken cancellationToken = default)
    {
        staffMember.SoftDelete();
        dbContext.Set<StaffMember>().Update(staffMember);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

