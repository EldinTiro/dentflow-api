using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Commands;

public record DeleteAllergyCommand(Guid PatientId, Guid AllergyId)
    : IRequest<ErrorOr<Deleted>>;
