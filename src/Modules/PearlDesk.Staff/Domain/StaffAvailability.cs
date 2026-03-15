using PearlDesk.Domain.Common;

namespace PearlDesk.Staff.Domain;

public class StaffAvailability : TenantAuditableEntity
{
    public Guid StaffMemberId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    // Navigation
    public StaffMember StaffMember { get; private set; } = default!;

    private StaffAvailability() { }

    public static StaffAvailability Create(
        Guid staffMemberId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null)
    {
        return new StaffAvailability
        {
            StaffMemberId = staffMemberId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }
}

