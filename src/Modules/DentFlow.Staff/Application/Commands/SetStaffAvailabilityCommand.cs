using ErrorOr;
using MediatR;

namespace DentFlow.Staff.Application.Commands;

public record SetStaffAvailabilityCommand(
    Guid StaffMemberId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<ErrorOr<StaffAvailabilityResponse>>;
