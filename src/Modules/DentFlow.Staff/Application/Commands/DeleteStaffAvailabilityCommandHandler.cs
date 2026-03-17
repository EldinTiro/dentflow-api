using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class DeleteStaffAvailabilityCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<DeleteStaffAvailabilityCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteStaffAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        var availability = await staffRepository.GetAvailabilityByIdAsync(command.AvailabilityId, cancellationToken);
        if (availability is null)
            return StaffErrors.AvailabilityNotFound;

        await staffRepository.SoftDeleteAvailabilityAsync(availability, cancellationToken);

        return Result.Deleted;
    }
}
