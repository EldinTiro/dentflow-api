using ErrorOr;
using MediatR;

namespace PearlDesk.Patients.Application.Queries;

public record GetPatientByIdQuery(Guid Id) : IRequest<ErrorOr<PatientResponse>>;

