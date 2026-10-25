using DentFlow.Domain.Tenants;

namespace DentFlow.Tenants.Application;

/// <summary>
/// The weekly schedule is the only source of opening hours (DT-13). The legacy
/// WorkdayStart/End columns are no longer sent: they disagreed with the schedule.
/// </summary>
public record ClinicSettingsResponse(
    int SlotDurationMinutes,
    string? WeeklyScheduleJson)
{
    public static ClinicSettingsResponse FromEntity(Tenant t) =>
        new(t.SlotDurationMinutes, t.WeeklyScheduleJson);
}
