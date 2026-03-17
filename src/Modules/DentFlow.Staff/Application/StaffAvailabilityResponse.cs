using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Application;

public record StaffAvailabilityResponse(
    Guid Id,
    Guid StaffMemberId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo)
{
    public static StaffAvailabilityResponse FromEntity(StaffAvailability a) => new(
        a.Id,
        a.StaffMemberId,
        a.DayOfWeek,
        a.StartTime,
        a.EndTime,
        a.EffectiveFrom,
        a.EffectiveTo);
}
