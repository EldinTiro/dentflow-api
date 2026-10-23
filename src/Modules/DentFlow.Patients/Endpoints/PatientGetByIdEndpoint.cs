using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Patients.Endpoints;

public class PatientGetByIdEndpoint(ISender sender) : EndpointWithoutRequest<PatientResponse>
{
    public override void Configure()
    {
        Get("/patients/{id}");
        Roles("ClinicOwner", "Receptionist", "Dentist", "DentalNurse", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Get a patient by ID");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetPatientByIdQuery(id), ct);
        if (result.IsError)
        {
            await this.SendResultErrorsAsync(result.Errors, ct);
            return;
        }
        await SendOkAsync(result.Value, ct);
    }
}
