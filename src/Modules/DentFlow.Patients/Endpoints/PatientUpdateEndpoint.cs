﻿using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Domain;
using DentFlow.Application.Common.Http;

namespace DentFlow.Patients.Endpoints;

public class PatientUpdateEndpoint(ISender sender) : Endpoint<UpdatePatientRequest, PatientResponse>
{
    public override void Configure()
    {
        Put("/patients/{id}");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Update a patient's details");
    }

    public override async Task HandleAsync(UpdatePatientRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var command = new UpdatePatientCommand(
            id, req.FirstName, req.LastName, req.PreferredName, req.ParentName,
            req.DateOfBirth, req.Gender,
            req.Pronouns, req.Email, req.PhoneMobile, req.PhoneHome, req.PhoneWork,
            req.PreferredContactMethod, req.AddressLine1, req.AddressLine2, req.City,
            req.StateProvince, req.PostalCode, req.CountryCode, req.Occupation,
            req.PreferredProviderId, req.SmsOptIn, req.EmailOptIn, req.Notes);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdatePatientRequest(
    string FirstName, string LastName, string? PreferredName, string? ParentName,
    DateOnly? DateOfBirth, Gender? Gender,
    string? Pronouns, string? Email, string? PhoneMobile, string? PhoneHome, string? PhoneWork,
    ContactMethod? PreferredContactMethod, string? AddressLine1, string? AddressLine2, string? City,
    string? StateProvince, string? PostalCode, string? CountryCode, string? Occupation,
    Guid? PreferredProviderId, bool SmsOptIn, bool EmailOptIn, string? Notes);


