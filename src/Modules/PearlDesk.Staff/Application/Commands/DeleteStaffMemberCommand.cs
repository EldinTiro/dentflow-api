using ErrorOr;
using MediatR;

namespace PearlDesk.Staff.Application.Commands;

public record DeleteStaffMemberCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;

