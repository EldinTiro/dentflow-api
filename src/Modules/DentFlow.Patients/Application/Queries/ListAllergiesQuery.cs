using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Queries;

public record ListAllergiesQuery(Guid PatientId)
    : IRequest<ErrorOr<IReadOnlyList<AllergyResponse>>>;
