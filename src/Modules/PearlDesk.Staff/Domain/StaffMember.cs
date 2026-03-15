﻿using PearlDesk.Domain.Common;

namespace PearlDesk.Staff.Domain;

public class StaffMember : TenantAuditableEntity
{
    public Guid? UserId { get; private set; }
    public StaffType StaffType { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public DateOnly? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? LicenseNumber { get; private set; }
    public DateOnly? LicenseExpiry { get; private set; }
    public string? NpiNumber { get; private set; }
    public string? Specialty { get; private set; }
    public string? ColorHex { get; private set; }
    public string? Biography { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly? HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }

    // Navigation
    public ICollection<StaffAvailability> Availabilities { get; private set; } = new List<StaffAvailability>();
    public ICollection<StaffBlockedTime> BlockedTimes { get; private set; } = new List<StaffBlockedTime>();

    private StaffMember() { }

    public static StaffMember Create(
        StaffType staffType,
        string firstName,
        string lastName,
        string? email,
        string? phone,
        DateOnly? hireDate,
        string? specialty = null,
        string? colorHex = null,
        Guid? userId = null)
    {
        return new StaffMember
        {
            StaffType = staffType,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            HireDate = hireDate,
            Specialty = specialty,
            ColorHex = colorHex ?? "#3B82F6",
            UserId = userId
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? email,
        string? phone,
        string? specialty,
        string? colorHex,
        string? biography,
        string? licenseNumber,
        DateOnly? licenseExpiry,
        string? npiNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Specialty = specialty;
        ColorHex = colorHex;
        Biography = biography;
        LicenseNumber = licenseNumber;
        LicenseExpiry = licenseExpiry;
        NpiNumber = npiNumber;
        SetUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        TerminationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        SetUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        TerminationDate = null;
        SetUpdated();
    }

    public string FullName => $"{FirstName} {LastName}";
}

