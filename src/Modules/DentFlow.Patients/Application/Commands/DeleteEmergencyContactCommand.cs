using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

public record DeleteEmergencyContactCommand(Guid PatientId, Guid ContactId)
    : IPatientScoped, IRequest<ErrorOr<Deleted>>;
