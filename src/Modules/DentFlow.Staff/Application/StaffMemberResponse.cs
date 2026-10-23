﻿using System.Text.Json.Serialization;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application;

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
    string? Address,
    string? City,
    string? PostalCode,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsBookable,
    // Only on the response that created the account, and only once; omitted elsewhere.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TempPassword = null)
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
        s.Address,
        s.City,
        s.PostalCode,
        s.CreatedAt,
        s.UpdatedAt,
        s.IsBookable);
}

