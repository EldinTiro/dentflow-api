using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Commands;

public record AddEmergencyContactCommand(
    Guid PatientId,
    string Name,
    string? Relationship,
    string? PhonePrimary,
    bool IsPrimary) : IRequest<ErrorOr<PatientEmergencyContactResponse>>;
