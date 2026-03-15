﻿using FastEndpoints;
using MediatR;
using PearlDesk.Patients.Application;
using PearlDesk.Patients.Application.Commands;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Endpoints;

public class PatientUpdateEndpoint(ISender sender) : Endpoint<UpdatePatientRequest, PatientResponse>
{
    public override void Configure()
    {
        Put("/patients/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Update a patient's details");
    }

    public override async Task HandleAsync(UpdatePatientRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var command = new UpdatePatientCommand(
            id, req.FirstName, req.LastName, req.PreferredName, req.DateOfBirth, req.Gender,
            req.Pronouns, req.Email, req.PhoneMobile, req.PhoneHome, req.PhoneWork,
            req.PreferredContactMethod, req.AddressLine1, req.AddressLine2, req.City,
            req.StateProvince, req.PostalCode, req.CountryCode, req.Occupation,
            req.PreferredProviderId, req.SmsOptIn, req.EmailOptIn, req.Notes);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdatePatientRequest(
    string FirstName, string LastName, string? PreferredName, DateOnly? DateOfBirth, Gender? Gender,
    string? Pronouns, string? Email, string? PhoneMobile, string? PhoneHome, string? PhoneWork,
    ContactMethod? PreferredContactMethod, string? AddressLine1, string? AddressLine2, string? City,
    string? StateProvince, string? PostalCode, string? CountryCode, string? Occupation,
    Guid? PreferredProviderId, bool SmsOptIn, bool EmailOptIn, string? Notes);


