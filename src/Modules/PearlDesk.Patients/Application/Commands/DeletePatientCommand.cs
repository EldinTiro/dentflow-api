using ErrorOr;
using MediatR;

namespace PearlDesk.Patients.Application.Commands;

public record DeletePatientCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;

