﻿using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Queries;
using DentFlow.Patients.Domain;
using DentFlow.Application.Common.Http;
using DomainRoles = DentFlow.Domain.Identity.Roles;

namespace DentFlow.Patients.Endpoints;

public class PatientListEndpoint(ISender sender) : EndpointWithoutRequest<PagedResult<PatientResponse>>
{
    public override void Configure()
    {
        Get("/patients");
        Roles([.. DomainRoles.TenantRoles]);
        Version(1);
        Summary(s => s.Summary = "List patients with optional search and status filter");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var search = Query<string?>("search", isRequired: false);
        var statusStr = Query<string?>("status", isRequired: false);
        var recallFilter = Query<string?>("recallFilter", isRequired: false);
        var pageStr = Query<string?>("page", isRequired: false);
        var pageSizeStr = Query<string?>("pageSize", isRequired: false);
        var page = int.TryParse(pageStr, out var p) ? p : 1;
        var pageSize = Math.Min(int.TryParse(pageSizeStr, out var ps) ? ps : 20, 100);

        PatientStatus? status = Enum.TryParse<PatientStatus>(statusStr, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        var result = await sender.Send(new ListPatientsQuery(search, status, recallFilter, page, pageSize), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
