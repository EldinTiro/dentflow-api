﻿using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Interfaces;

public interface IStaffRepository
{
    Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StaffMember?> GetByEmailAsync(string? email, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StaffMember> Items, int Total)> ListAsync(
        StaffType? staffType,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(StaffMember staffMember, CancellationToken cancellationToken = default);
    Task UpdateAsync(StaffMember staffMember, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(StaffMember staffMember, CancellationToken cancellationToken = default);

    // Availability
    Task<IReadOnlyList<StaffAvailability>> GetAvailabilitiesAsync(Guid staffMemberId, CancellationToken cancellationToken = default);
    Task<StaffAvailability?> GetAvailabilityByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAvailabilityAsync(StaffAvailability availability, CancellationToken cancellationToken = default);
    Task SoftDeleteAvailabilityAsync(StaffAvailability availability, CancellationToken cancellationToken = default);

    // Blocked Times
    Task<IReadOnlyList<StaffBlockedTime>> GetBlockedTimesAsync(Guid staffMemberId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
    Task<StaffBlockedTime?> GetBlockedTimeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddBlockedTimeAsync(StaffBlockedTime blockedTime, CancellationToken cancellationToken = default);
    Task SoftDeleteBlockedTimeAsync(StaffBlockedTime blockedTime, CancellationToken cancellationToken = default);
}

