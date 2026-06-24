using DentFlow.Domain.Tenants;

namespace DentFlow.Tenants.Application;

public record ClinicSettingsResponse(
    string WorkdayStart,
    string WorkdayEnd,
    int SlotDurationMinutes,
    string? WeeklyScheduleJson)
{
    public static ClinicSettingsResponse FromEntity(Tenant t) =>
        new(
            t.WorkdayStart.ToString("HH:mm"),
            t.WorkdayEnd.ToString("HH:mm"),
            t.SlotDurationMinutes,
            t.WeeklyScheduleJson);
}
