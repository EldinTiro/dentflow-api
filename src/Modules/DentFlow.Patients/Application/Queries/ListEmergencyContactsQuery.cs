using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Queries;

public record ListEmergencyContactsQuery(Guid PatientId)
    : IPatientScoped, IRequest<ErrorOr<IReadOnlyList<PatientEmergencyContactResponse>>>;
