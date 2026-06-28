using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Queries;

namespace DentFlow.Patients.Endpoints;

public class ListAllergiesEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<AllergyResponse>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/allergies");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(new ListAllergiesQuery(patientId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public class AddAllergyRequest
{
    public string Allergen { get; set; } = default!;
    public string? Reaction { get; set; }
    public string? Severity { get; set; }
    public string? Notes { get; set; }
}

public class AddAllergyEndpoint(ISender sender)
    : Endpoint<AddAllergyRequest, AllergyResponse>
{
    public override void Configure()
    {
        Post("/patients/{patientId}/allergies");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(AddAllergyRequest req, CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(
            new AddAllergyCommand(patientId, req.Allergen, req.Reaction, req.Severity, req.Notes), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendCreatedAtAsync<ListAllergiesEndpoint>(null, result.Value, cancellation: ct);
    }
}

public class DeleteAllergyEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{patientId}/allergies/{allergyId}");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var allergyId = Route<Guid>("allergyId");
        var result = await sender.Send(new DeleteAllergyCommand(patientId, allergyId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendNoContentAsync(ct);
    }
}
