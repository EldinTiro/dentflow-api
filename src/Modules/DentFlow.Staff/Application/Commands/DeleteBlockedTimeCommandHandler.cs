using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class DeleteBlockedTimeCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<DeleteBlockedTimeCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteBlockedTimeCommand command,
        CancellationToken cancellationToken)
    {
        var blockedTime = await staffRepository.GetBlockedTimeByIdAsync(command.BlockedTimeId, cancellationToken);
        if (blockedTime is null)
            return StaffErrors.BlockedTimeNotFound;

        await staffRepository.SoftDeleteBlockedTimeAsync(blockedTime, cancellationToken);

        return Result.Deleted;
    }
}
