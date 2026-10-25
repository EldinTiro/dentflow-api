using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

public record AddAllergyCommand(
    Guid PatientId,
    string Allergen,
    string? Reaction,
    string? Severity,
    string? Notes) : IPatientScoped, IRequest<ErrorOr<AllergyResponse>>;
