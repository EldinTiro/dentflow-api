using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Patients.Endpoints;

public class PatientDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Soft-delete a patient");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new DeletePatientCommand(id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
