﻿using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application;

public record StaffMemberResponse(
    Guid Id,
    StaffType StaffType,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    string? Specialty,
    string? ColorHex,
    string? Biography,
    string? LicenseNumber,
    DateOnly? LicenseExpiry,
    string? NpiNumber,
    bool IsActive,
    DateOnly? HireDate,
    DateOnly? TerminationDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static StaffMemberResponse FromEntity(StaffMember s) => new(
        s.Id,
        s.StaffType,
        s.FirstName,
        s.LastName,
        s.FullName,
        s.Email,
        s.Phone,
        s.Specialty,
        s.ColorHex,
        s.Biography,
        s.LicenseNumber,
        s.LicenseExpiry,
        s.NpiNumber,
        s.IsActive,
        s.HireDate,
        s.TerminationDate,
        s.CreatedAt,
        s.UpdatedAt);
}

