using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application.Commands;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Patients.Endpoints;

public class PatientPurgeEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{id}/permanent");
        Policies(Permission.RecordsPurge);
        Version(1);
        Summary(s => s.Summary = "Delete a patient for good, with visits, invoices, plans, tooth chart and files (Administrator)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new PurgePatientCommand(Route<Guid>("id")), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
