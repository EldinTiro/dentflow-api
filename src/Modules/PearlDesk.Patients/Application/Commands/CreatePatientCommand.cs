﻿using ErrorOr;
using MediatR;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Commands;

public record CreatePatientCommand(
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    string? Email,
    string? PhoneMobile,
    string? PhoneHome,
    string? PhoneWork,
    ContactMethod? PreferredContactMethod,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? CountryCode,
    string? Occupation,
    Guid? PreferredProviderId,
    bool SmsOptIn,
    bool EmailOptIn,
    string? ReferredBySource,
    string? Notes) : IRequest<ErrorOr<PatientResponse>>;

