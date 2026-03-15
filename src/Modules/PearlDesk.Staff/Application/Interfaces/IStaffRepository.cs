﻿using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Interfaces;

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
}

