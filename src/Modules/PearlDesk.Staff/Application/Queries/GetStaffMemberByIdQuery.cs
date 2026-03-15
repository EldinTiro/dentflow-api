using ErrorOr;
using MediatR;

namespace PearlDesk.Staff.Application.Queries;

public record GetStaffMemberByIdQuery(Guid Id) : IRequest<ErrorOr<StaffMemberResponse>>;

