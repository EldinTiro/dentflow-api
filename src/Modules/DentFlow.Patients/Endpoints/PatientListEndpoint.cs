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
        Summary(s => s.Summary = "List patients with optional search and status filter (default Active; status=all for every status)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var search = Query<string?>("search", isRequired: false);
        var statusStr = Query<string?>("status", isRequired: false);
        var recallFilter = Query<string?>("recallFilter", isRequired: false);
        var pageStr = Query<string?>("page", isRequired: false);
        var pageSizeStr = Query<string?>("pageSize", isRequired: false);
        var (page, pageSize) = Paging.Normalize(pageStr, pageSizeStr, defaultSize: 20, max: 100);

        // People-merge plan D11: Active unless asked otherwise; "all" lifts the filter.
        PatientStatus? status = string.Equals(statusStr, "all", StringComparison.OrdinalIgnoreCase)
            ? null
            : Enum.TryParse<PatientStatus>(statusStr, ignoreCase: true, out var parsed) ? parsed : PatientStatus.Active;

        var result = await sender.Send(new ListPatientsQuery(search, status, recallFilter, page, pageSize), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
