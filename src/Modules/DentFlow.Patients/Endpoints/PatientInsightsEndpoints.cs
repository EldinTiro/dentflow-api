using FastEndpoints;
using MediatR;
using DentFlow.Application.Common.Http;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Queries;
using DomainRoles = DentFlow.Domain.Identity.Roles;

namespace DentFlow.Patients.Endpoints;

public class PatientTimelineEndpoint(ISender sender) : EndpointWithoutRequest<IReadOnlyList<PatientTimelineEntry>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/timeline");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "Everything that happened to a patient, newest first");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetPatientTimelineQuery(Route<Guid>("patientId")), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
