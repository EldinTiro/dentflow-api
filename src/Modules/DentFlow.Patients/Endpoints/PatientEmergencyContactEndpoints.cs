using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Queries;

namespace DentFlow.Patients.Endpoints;

public class ListEmergencyContactsEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<PatientEmergencyContactResponse>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/emergency-contacts");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(new ListEmergencyContactsQuery(patientId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public class AddEmergencyContactRequest
{
    public string Name { get; set; } = default!;
    public string? Relationship { get; set; }
    public string? PhonePrimary { get; set; }
    public bool IsPrimary { get; set; }
}

public class AddEmergencyContactEndpoint(ISender sender)
    : Endpoint<AddEmergencyContactRequest, PatientEmergencyContactResponse>
{
    public override void Configure()
    {
        Post("/patients/{patientId}/emergency-contacts");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(AddEmergencyContactRequest req, CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(
            new AddEmergencyContactCommand(patientId, req.Name, req.Relationship, req.PhonePrimary, req.IsPrimary), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendCreatedAtAsync<ListEmergencyContactsEndpoint>(null, result.Value, cancellation: ct);
    }
}

public class DeleteEmergencyContactEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{patientId}/emergency-contacts/{contactId}");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var contactId = Route<Guid>("contactId");
        var result = await sender.Send(new DeleteEmergencyContactCommand(patientId, contactId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendNoContentAsync(ct);
    }
}
