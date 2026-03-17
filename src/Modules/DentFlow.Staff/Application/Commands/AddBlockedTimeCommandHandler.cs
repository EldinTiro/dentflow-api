using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class AddBlockedTimeCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<AddBlockedTimeCommand, ErrorOr<StaffBlockedTimeResponse>>
{
    public async Task<ErrorOr<StaffBlockedTimeResponse>> Handle(
        AddBlockedTimeCommand command,
        CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(command.StaffMemberId, cancellationToken);
        if (staff is null)
            return StaffErrors.NotFound;

        var blockedTime = StaffBlockedTime.Create(
            command.StaffMemberId,
            command.StartAt,
            command.EndAt,
            command.Reason,
            command.Notes);

        await staffRepository.AddBlockedTimeAsync(blockedTime, cancellationToken);

        return StaffBlockedTimeResponse.FromEntity(blockedTime);
    }
}
