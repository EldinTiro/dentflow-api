using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

public record AddEmergencyContactCommand(
    Guid PatientId,
    string Name,
    string? Relationship,
    string? PhonePrimary,
    bool IsPrimary) : IPatientScoped, IRequest<ErrorOr<PatientEmergencyContactResponse>>;
