using ErrorOr;
using MediatR;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application.Commands;

public class SetStaffAvailabilityCommandHandler(IStaffRepository staffRepository)
    : IRequestHandler<SetStaffAvailabilityCommand, ErrorOr<StaffAvailabilityResponse>>
{
    public async Task<ErrorOr<StaffAvailabilityResponse>> Handle(
        SetStaffAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(command.StaffMemberId, cancellationToken);
        if (staff is null)
            return StaffErrors.NotFound;

        // Check for overlapping slot on the same day
        var existing = await staffRepository.GetAvailabilitiesAsync(command.StaffMemberId, cancellationToken);
        var hasConflict = existing.Any(a =>
            a.DayOfWeek == command.DayOfWeek &&
            a.StartTime < command.EndTime &&
            a.EndTime > command.StartTime);

        if (hasConflict)
            return StaffErrors.ConflictingAvailabilitySlot;

        var availability = StaffAvailability.Create(
            command.StaffMemberId,
            command.DayOfWeek,
            command.StartTime,
            command.EndTime,
            command.EffectiveFrom,
            command.EffectiveTo);

        await staffRepository.AddAvailabilityAsync(availability, cancellationToken);

        return StaffAvailabilityResponse.FromEntity(availability);
    }
}
