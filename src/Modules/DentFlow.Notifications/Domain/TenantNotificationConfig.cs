using DentFlow.Domain.Common;

namespace DentFlow.Notifications.Domain;

public class TenantNotificationConfig : TenantAuditableEntity
{
    public bool SmsEnabled { get; private set; }
    public int? Reminder1HoursBefore { get; private set; }
    public int? Reminder2HoursBefore { get; private set; }

    private TenantNotificationConfig() { }

    public static TenantNotificationConfig CreateDefault(Guid tenantId)
    {
        var config = new TenantNotificationConfig
        {
            SmsEnabled = false,
            Reminder1HoursBefore = 24,
            Reminder2HoursBefore = null
        };
        config.SetTenant(tenantId);
        return config;
    }

    public void Update(bool smsEnabled, int? reminder1HoursBefore, int? reminder2HoursBefore)
    {
        SmsEnabled = smsEnabled;
        Reminder1HoursBefore = reminder1HoursBefore;
        Reminder2HoursBefore = reminder2HoursBefore;
        SetUpdated();
    }
}
