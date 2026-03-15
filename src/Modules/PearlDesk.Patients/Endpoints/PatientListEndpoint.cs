﻿using FastEndpoints;
using MediatR;
using PearlDesk.Patients.Application;
using PearlDesk.Patients.Application.Queries;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Endpoints;

public class PatientListEndpoint(ISender sender) : EndpointWithoutRequest<PagedResult<PatientResponse>>
{
    public override void Configure()
    {
        Get("/patients");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "List patients with optional search and status filter");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var search = Query<string?>("search", isRequired: false);
        var statusStr = Query<string?>("status", isRequired: false);
        var pageStr = Query<string?>("page", isRequired: false);
        var pageSizeStr = Query<string?>("pageSize", isRequired: false);
        var page = int.TryParse(pageStr, out var p) ? p : 1;
        var pageSize = Math.Min(int.TryParse(pageSizeStr, out var ps) ? ps : 20, 100);

        PatientStatus? status = Enum.TryParse<PatientStatus>(statusStr, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        var result = await sender.Send(new ListPatientsQuery(search, status, page, pageSize), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
