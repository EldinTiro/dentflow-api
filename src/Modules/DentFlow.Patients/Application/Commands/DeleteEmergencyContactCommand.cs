using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Commands;

public record DeleteEmergencyContactCommand(Guid PatientId, Guid ContactId)
    : IRequest<ErrorOr<Deleted>>;
