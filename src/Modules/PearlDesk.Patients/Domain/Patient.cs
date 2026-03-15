﻿using PearlDesk.Domain.Common;

namespace PearlDesk.Patients.Domain;

public class Patient : TenantAuditableEntity
{
    public Guid? UserId { get; private set; }
    public string PatientNumber { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? PreferredName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    public string? Pronouns { get; private set; }
    public string? MaritalStatus { get; private set; }
    public string? Occupation { get; private set; }
    public string? PhoneMobile { get; private set; }
    public string? PhoneHome { get; private set; }
    public string? PhoneWork { get; private set; }
    public string? Email { get; private set; }
    public ContactMethod? PreferredContactMethod { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? StateProvince { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public string? LanguagePreference { get; private set; }
    public Guid? ReferredByPatientId { get; private set; }
    public string? ReferredBySource { get; private set; }
    public Guid? PreferredProviderId { get; private set; }
    public PatientStatus Status { get; private set; } = PatientStatus.Active;
    public DateOnly? FirstVisitDate { get; private set; }
    public DateOnly? LastVisitDate { get; private set; }
    public DateOnly? RecallDueDate { get; private set; }
    public string? Notes { get; private set; }
    public bool PortalOptIn { get; private set; }
    public bool SmsOptIn { get; private set; }
    public bool EmailOptIn { get; private set; } = true;

    // Navigation
    public ICollection<PatientEmergencyContact> EmergencyContacts { get; private set; } = new List<PatientEmergencyContact>();
    public ICollection<Allergy> Allergies { get; private set; } = new List<Allergy>();
    public ICollection<MedicalHistory> MedicalHistories { get; private set; } = new List<MedicalHistory>();

    private Patient() { }

    public static Patient Create(
        string patientNumber,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        string? email,
        string? phoneMobile,
        Gender? gender = null)
    {
        return new Patient
        {
            PatientNumber = patientNumber,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Email = email,
            PhoneMobile = phoneMobile,
            Gender = gender
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? preferredName,
        DateOnly? dateOfBirth,
        Gender? gender,
        string? pronouns,
        string? email,
        string? phoneMobile,
        string? phoneHome,
        string? phoneWork,
        ContactMethod? preferredContactMethod,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateProvince,
        string? postalCode,
        string? countryCode,
        string? occupation,
        Guid? preferredProviderId,
        bool smsOptIn,
        bool emailOptIn,
        string? notes)
    {
        FirstName = firstName;
        LastName = lastName;
        PreferredName = preferredName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Pronouns = pronouns;
        Email = email;
        PhoneMobile = phoneMobile;
        PhoneHome = phoneHome;
        PhoneWork = phoneWork;
        PreferredContactMethod = preferredContactMethod;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateProvince = stateProvince;
        PostalCode = postalCode;
        CountryCode = countryCode;
        Occupation = occupation;
        PreferredProviderId = preferredProviderId;
        SmsOptIn = smsOptIn;
        EmailOptIn = emailOptIn;
        Notes = notes;
        SetUpdated();
    }

    public void UpdateStatus(PatientStatus status)
    {
        Status = status;
        SetUpdated();
    }

    public void UpdateLastVisit(DateOnly visitDate)
    {
        LastVisitDate = visitDate;
        if (FirstVisitDate is null) FirstVisitDate = visitDate;
        SetUpdated();
    }

    public string FullName => $"{FirstName} {LastName}";
    public string DisplayName => string.IsNullOrWhiteSpace(PreferredName)
        ? FullName
        : $"{PreferredName} {LastName}";
}

