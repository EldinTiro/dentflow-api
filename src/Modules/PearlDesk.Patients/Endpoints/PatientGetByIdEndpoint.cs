using FastEndpoints;
using MediatR;
using PearlDesk.Patients.Application;
using PearlDesk.Patients.Application.Queries;

namespace PearlDesk.Patients.Endpoints;

public class PatientGetByIdEndpoint(ISender sender) : EndpointWithoutRequest<PatientResponse>
{
    public override void Configure()
    {
        Get("/patients/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get a patient by ID");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetPatientByIdQuery(id), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
