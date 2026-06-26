using DentFlow.Notifications.Domain;

namespace DentFlow.Notifications.Application;

public record NotificationConfigResponse(
    bool SmsEnabled,
    int? Reminder1HoursBefore,
    int? Reminder2HoursBefore)
{
    public static NotificationConfigResponse FromEntity(TenantNotificationConfig config) =>
        new(config.SmsEnabled, config.Reminder1HoursBefore, config.Reminder2HoursBefore);

    public static NotificationConfigResponse Default() =>
        new(false, 24, null);
}
