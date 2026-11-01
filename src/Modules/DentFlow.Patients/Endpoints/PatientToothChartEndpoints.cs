using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Queries;
using DentFlow.Application.Common.Http;
using DomainRoles = DentFlow.Domain.Identity.Roles;

namespace DentFlow.Patients.Endpoints;

public class ListToothFindingsEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<ToothFindingResponse>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/teeth");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "The patient's tooth chart (FDI numbering)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new ListToothFindingsQuery(Route<Guid>("patientId")), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public record AddToothFindingRequest(int Tooth, string Condition, string? Surfaces, string? Notes);

public class AddToothFindingEndpoint(ISender sender)
    : Endpoint<AddToothFindingRequest, ToothFindingResponse>
{
    public override void Configure()
    {
        Post("/patients/{patientId}/teeth");
        // Clinical roles, like treatment plans (DT-12): the desk reads the chart, clinicians write it
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "Record a finding on a tooth: caries, filling, crown, root canal, implant, missing");
    }

    public override async Task HandleAsync(AddToothFindingRequest req, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddToothFindingCommand(Route<Guid>("patientId"), req.Tooth, req.Condition, req.Surfaces, req.Notes), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendCreatedAtAsync<ListToothFindingsEndpoint>(new { patientId = Route<Guid>("patientId") }, result.Value, cancellation: ct);
    }
}

public class DeleteToothFindingEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/patients/{patientId}/teeth/{findingId}");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new DeleteToothFindingCommand(Route<Guid>("patientId"), Route<Guid>("findingId")), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
