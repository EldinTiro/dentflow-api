﻿using FastEndpoints;
using MediatR;
using PearlDesk.Patients.Application;
using PearlDesk.Patients.Application.Commands;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Endpoints;

public class PatientCreateEndpoint(ISender sender) : Endpoint<CreatePatientRequest, PatientResponse>
{
    public override void Configure()
    {
        Post("/patients");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Create a new patient");
    }

    public override async Task HandleAsync(CreatePatientRequest req, CancellationToken ct)
    {
        var command = new CreatePatientCommand(
            req.FirstName, req.LastName, req.PreferredName, req.DateOfBirth, req.Gender,
            req.Email, req.PhoneMobile, req.PhoneHome, req.PhoneWork, req.PreferredContactMethod,
            req.AddressLine1, req.AddressLine2, req.City, req.StateProvince, req.PostalCode,
            req.CountryCode, req.Occupation, req.PreferredProviderId, req.SmsOptIn, req.EmailOptIn,
            req.ReferredBySource, req.Notes);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendCreatedAtAsync<PatientGetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

public record CreatePatientRequest(
    string FirstName, string LastName, string? PreferredName, DateOnly? DateOfBirth, Gender? Gender,
    string? Email, string? PhoneMobile, string? PhoneHome, string? PhoneWork, ContactMethod? PreferredContactMethod,
    string? AddressLine1, string? AddressLine2, string? City, string? StateProvince, string? PostalCode,
    string? CountryCode, string? Occupation, Guid? PreferredProviderId, bool SmsOptIn, bool EmailOptIn,
    string? ReferredBySource, string? Notes);


