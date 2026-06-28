using FastEndpoints;
using MediatR;
using DentFlow.Patients.Application;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Queries;

namespace DentFlow.Patients.Endpoints;

public class GetMedicalHistoryEndpoint(ISender sender)
    : EndpointWithoutRequest<MedicalHistoryResponse?>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/medical-history");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(new GetMedicalHistoryQuery(patientId), ct);
        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        if (result.Value is null) { await SendNoContentAsync(ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public class UpsertMedicalHistoryRequest
{
    public string? BloodType { get; set; }
    public bool? IsPregnant { get; set; }
    public bool IsSmoker { get; set; }
    public bool IsDiabetic { get; set; }
    public bool HasHeartCondition { get; set; }
    public bool HasHypertension { get; set; }
    public bool HasBleedingDisorder { get; set; }
    public bool IsOnBloodThinners { get; set; }
    public bool HasPacemaker { get; set; }
    public bool HasArtificialJoints { get; set; }
    public bool HasLatexAllergy { get; set; }
    public string? GeneralNotes { get; set; }
    public string? CurrentMedications { get; set; }
    public string? PhysicianName { get; set; }
    public string? PhysicianPhone { get; set; }
}

public class UpsertMedicalHistoryEndpoint(ISender sender)
    : Endpoint<UpsertMedicalHistoryRequest, MedicalHistoryResponse>
{
    public override void Configure()
    {
        Put("/patients/{patientId}/medical-history");
        Roles("ClinicOwner", "ClinicAdmin", "Receptionist", "Dentist", "Hygienist", "SuperAdmin");
        Version(1);
    }

    public override async Task HandleAsync(UpsertMedicalHistoryRequest req, CancellationToken ct)
    {
        var patientId = Route<Guid>("patientId");
        var result = await sender.Send(new UpsertMedicalHistoryCommand(
            patientId,
            req.BloodType,
            req.IsPregnant,
            req.IsSmoker,
            req.IsDiabetic,
            req.HasHeartCondition,
            req.HasHypertension,
            req.HasBleedingDisorder,
            req.IsOnBloodThinners,
            req.HasPacemaker,
            req.HasArtificialJoints,
            req.HasLatexAllergy,
            req.GeneralNotes,
            req.CurrentMedications,
            req.PhysicianName,
            req.PhysicianPhone), ct);

        if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
