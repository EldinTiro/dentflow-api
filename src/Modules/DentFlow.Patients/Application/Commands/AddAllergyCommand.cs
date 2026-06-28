using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Commands;

public record AddAllergyCommand(
    Guid PatientId,
    string Allergen,
    string? Reaction,
    string? Severity,
    string? Notes) : IRequest<ErrorOr<AllergyResponse>>;
