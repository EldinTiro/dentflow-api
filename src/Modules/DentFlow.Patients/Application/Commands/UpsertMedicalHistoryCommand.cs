using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Commands;

public record UpsertMedicalHistoryCommand(
    Guid PatientId,
    string? BloodType,
    bool? IsPregnant,
    bool IsSmoker,
    bool IsDiabetic,
    bool HasHeartCondition,
    bool HasHypertension,
    bool HasBleedingDisorder,
    bool IsOnBloodThinners,
    bool HasPacemaker,
    bool HasArtificialJoints,
    bool HasLatexAllergy,
    string? GeneralNotes,
    string? CurrentMedications,
    string? PhysicianName,
    string? PhysicianPhone) : IRequest<ErrorOr<MedicalHistoryResponse>>;
