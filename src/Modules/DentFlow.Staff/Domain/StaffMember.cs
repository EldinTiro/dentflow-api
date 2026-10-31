﻿using DentFlow.Domain.Common;
using ErrorOr;

namespace DentFlow.Staff.Domain;

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
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
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
        string? colorHex = null)
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
        string? npiNumber,
        string? address,
        string? city,
        string? postalCode)
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
        Address = address;
        City = city;
        PostalCode = postalCode;
        SetUpdated();
    }

    public void Rename(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        SetUpdated();
    }

    public ErrorOr<Success> Deactivate()
    {
        if (!IsActive) return StaffErrors.AlreadyInactive;

        IsActive = false;
        TerminationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        SetUpdated();
        return Result.Success;
    }

    public ErrorOr<Success> Activate()
    {
        if (IsActive) return StaffErrors.AlreadyActive;

        IsActive = true;
        TerminationDate = null;
        SetUpdated();
        return Result.Success;
    }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// The staff type for a profile made from an account (backfill, provisioning). Owners
    /// who don't treat patients get a non-bookable profile (people-merge D3).
    /// </summary>
    public static StaffType TypeForAccountRole(string? role) => role switch
    {
        DentFlow.Domain.Identity.Roles.Dentist => StaffType.Dentist,
        DentFlow.Domain.Identity.Roles.DentalNurse => StaffType.DentalAssistant,
        DentFlow.Domain.Identity.Roles.Receptionist => StaffType.Receptionist,
        _ => StaffType.OfficeManager,
    };

    /// <summary>
    /// Can appear in provider lists and be booked (people-merge plan D3). Decided here,
    /// not in the web app: clinical people only, and never while inactive.
    /// </summary>
    public bool IsBookable => IsActive && IsClinical(StaffType);

    /// <summary>The staff types that treat patients and so can be booked.</summary>
    public static bool IsClinical(StaffType type) =>
        type is StaffType.Dentist or StaffType.Hygienist or StaffType.DentalAssistant;

    /// <summary>
    /// What this person does. Independent of their sign-in role: an owner can be a dentist too
    /// (the role decides permissions, the type decides who can be booked).
    /// </summary>
    public void ChangeType(StaffType type)
    {
        if (StaffType == type) return;
        StaffType = type;
        SetUpdated();
    }

    /// <summary>
    /// Ties a sign-in account to this profile. The profile's email is the login email (D1),
    /// so it must exist; one profile holds at most one account.
    /// </summary>
    public ErrorOr<Success> LinkUser(Guid userId)
    {
        var allowed = CanLinkUser();
        if (allowed.IsError) return allowed.Errors;

        UserId = userId;
        SetUpdated();
        return Result.Success;
    }

    /// <summary>Same rules as <see cref="LinkUser"/>, checked before an account is created for it.</summary>
    public ErrorOr<Success> CanLinkUser()
    {
        if (UserId is not null) return StaffErrors.AlreadyHasAccess;
        if (!IsActive) return StaffErrors.Inactive;
        if (string.IsNullOrWhiteSpace(Email)) return StaffErrors.EmailRequiredForAccess;
        return Result.Success;
    }

    /// <summary>
    /// Backfill only (people-merge plan 3.1): attaches an account that already exists to its
    /// profile even when one of them is inactive — history is linked, not refused.
    /// </summary>
    public ErrorOr<Success> AttachExistingAccount(Guid userId)
    {
        if (UserId is not null) return StaffErrors.AlreadyHasAccess;
        UserId = userId;
        SetUpdated();
        return Result.Success;
    }

    public void Unlink()
    {
        UserId = null;
        SetUpdated();
    }
}

