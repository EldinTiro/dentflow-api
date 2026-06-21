using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application.Queries;

namespace DentFlow.Patients.Endpoints;

public class PatientSearchEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyList<PatientSearchResult>>
{
    public override void Configure()
    {
        Get("/patients/search");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Quick patient autocomplete search by name, phone or email. Returns up to 20 results.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var q = Query<string?>("q", isRequired: false);
        var limit = Query<int?>("limit", isRequired: false) ?? 20;

        var result = await sender.Send(new SearchPatientsQuery(q, limit), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
