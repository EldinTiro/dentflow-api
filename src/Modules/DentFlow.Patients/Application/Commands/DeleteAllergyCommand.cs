using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

public record DeleteAllergyCommand(Guid PatientId, Guid AllergyId)
    : IPatientScoped, IRequest<ErrorOr<Deleted>>;
