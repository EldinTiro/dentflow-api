using ErrorOr;
using MediatR;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Application.Commands;

public class DeleteStaffMemberCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<DeleteStaffMemberCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteStaffMemberCommand command,
        CancellationToken cancellationToken)
    {
        var staffMember = await staffRepository.GetByIdAsync(command.Id, cancellationToken);
        if (staffMember is null)
            return StaffErrors.NotFound;

        await staffRepository.SoftDeleteAsync(staffMember, cancellationToken);

        return Result.Deleted;
    }
}

