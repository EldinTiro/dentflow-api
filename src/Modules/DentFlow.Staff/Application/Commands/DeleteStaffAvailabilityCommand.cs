using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Commands;

public record DeleteStaffAvailabilityCommand(Guid AvailabilityId) : IRequest<ErrorOr<Deleted>>;
