using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Queries;

public record ListAllergiesQuery(Guid PatientId)
    : IPatientScoped, IRequest<ErrorOr<IReadOnlyList<AllergyResponse>>>;
