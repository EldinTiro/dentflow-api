using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Commands;

public record AddBlockedTimeCommand(
    Guid StaffMemberId,
    DateTime StartAt,
    DateTime EndAt,
    string? Reason,
    string? Notes) : IRequest<ErrorOr<StaffBlockedTimeResponse>>;
