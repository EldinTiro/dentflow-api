using ErrorOr;

namespace DentFlow.Notifications.Domain;

public static class NotificationErrors
{
    public static readonly Error ConfigNotFound =
        Error.NotFound("Notification.ConfigNotFound", "Notification configuration not found.");

    public static readonly Error LogNotFound =
        Error.NotFound("Notification.LogNotFound", "Notification log entry not found.");

    public static readonly Error PatientNotOptedIn =
        Error.Validation("Notification.PatientNotOptedIn", "Patient has not opted in to SMS notifications.");

    public static readonly Error FeatureDisabled =
        Error.Forbidden("Notification.FeatureDisabled", "SMS notifications are not enabled on this plan.");
}
