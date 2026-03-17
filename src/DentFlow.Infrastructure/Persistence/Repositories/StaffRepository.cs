﻿using Microsoft.EntityFrameworkCore;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Infrastructure.Persistence.Repositories;

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

    // Availability
    public async Task<IReadOnlyList<StaffAvailability>> GetAvailabilitiesAsync(Guid staffMemberId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<StaffAvailability>()
            .Where(a => a.StaffMemberId == staffMemberId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<StaffAvailability?> GetAvailabilityByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<StaffAvailability>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAvailabilityAsync(StaffAvailability availability, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<StaffAvailability>().AddAsync(availability, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAvailabilityAsync(StaffAvailability availability, CancellationToken cancellationToken = default)
    {
        availability.SoftDelete();
        dbContext.Set<StaffAvailability>().Update(availability);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Blocked Times
    public async Task<IReadOnlyList<StaffBlockedTime>> GetBlockedTimesAsync(
        Guid staffMemberId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<StaffBlockedTime>()
            .Where(b => b.StaffMemberId == staffMemberId);

        if (from.HasValue)
        {
            var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(b => b.EndAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(b => b.StartAt <= toUtc);
        }

        return await query.OrderBy(b => b.StartAt).ToListAsync(cancellationToken);
    }

    public async Task<StaffBlockedTime?> GetBlockedTimeByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<StaffBlockedTime>().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task AddBlockedTimeAsync(StaffBlockedTime blockedTime, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<StaffBlockedTime>().AddAsync(blockedTime, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteBlockedTimeAsync(StaffBlockedTime blockedTime, CancellationToken cancellationToken = default)
    {
        blockedTime.SoftDelete();
        dbContext.Set<StaffBlockedTime>().Update(blockedTime);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

