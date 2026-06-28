using ErrorOr;
using MediatR;

namespace DentFlow.Patients.Application.Queries;

public record ListEmergencyContactsQuery(Guid PatientId)
    : IRequest<ErrorOr<IReadOnlyList<PatientEmergencyContactResponse>>>;
