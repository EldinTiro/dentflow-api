﻿using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Domain;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Patients.Endpoints;

public class PatientCreateEndpoint(ISender sender) : Endpoint<CreatePatientRequest, PatientResponse>
{
    public override void Configure()
    {
        Post("/patients");
        Policies(Permission.PatientsManage);
        Version(1);
        Summary(s => s.Summary = "Create a new patient");
    }

    public override async Task HandleAsync(CreatePatientRequest req, CancellationToken ct)
    {
        var command = new CreatePatientCommand(
            req.PatientNumber, req.FirstName, req.LastName, req.PreferredName, req.ParentName,
            req.DateOfBirth, req.Gender,
            req.Email, req.PhoneMobile, req.PhoneHome, req.PhoneWork, req.PreferredContactMethod,
            req.AddressLine1, req.AddressLine2, req.City, req.StateProvince, req.PostalCode,
            req.CountryCode, req.Occupation, req.PreferredProviderId, req.SmsOptIn, req.EmailOptIn,
            req.ReferredBySource, req.Notes);

        var result = await sender.Send(command, ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendCreatedAtAsync<PatientGetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

public record CreatePatientRequest(
    string? PatientNumber,
    string FirstName, string LastName, string? PreferredName, string? ParentName,
    DateOnly? DateOfBirth, Gender? Gender,
    string? Email, string? PhoneMobile, string? PhoneHome, string? PhoneWork, ContactMethod? PreferredContactMethod,
    string? AddressLine1, string? AddressLine2, string? City, string? StateProvince, string? PostalCode,
    string? CountryCode, string? Occupation, Guid? PreferredProviderId, bool SmsOptIn, bool EmailOptIn,
    string? ReferredBySource, string? Notes);


