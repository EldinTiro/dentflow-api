using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application.Commands;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Patients.Endpoints;

public class PatientDeleteEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{id}");
        Policies(Permission.PatientsManage);
        Version(1);
        Summary(s => s.Summary = "Soft-delete a duplicate patient; 409 Patient.HasHistory once there is anything to keep");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new DeletePatientCommand(id), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
