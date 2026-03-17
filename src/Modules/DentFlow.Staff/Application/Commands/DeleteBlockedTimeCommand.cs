using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Commands;

public record DeleteBlockedTimeCommand(Guid BlockedTimeId) : IRequest<ErrorOr<Deleted>>;
