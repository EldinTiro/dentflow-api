using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;
using DentFlow.Appointments.Domain;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Notifications.Application.Interfaces;
using DentFlow.Notifications.Domain;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Cross-tenant Hangfire job. Scans all SMS-enabled tenants for appointments
/// due for a reminder and sends them via INotificationChannel.
/// Uses IgnoreQueryFilters() because it intentionally operates across all tenants.
/// </summary>
public sealed class AppointmentReminderJob(
    ApplicationDbContext db,
    INotificationChannel channel,
    INotificationLogRepository logRepository,
    ILogger<AppointmentReminderJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var configs = await db.TenantNotificationConfigs
            .IgnoreQueryFilters()
            .Where(c => c.SmsEnabled && !c.IsDeleted)
            .ToListAsync(ct);

        if (configs.Count == 0)
            return;

        var realTenantIds = configs
            .Select(c => c.TenantId)
            .Where(id => id != Guid.Empty)
            .ToList();

        var tenants = await db.Tenants
            .Where(t => realTenantIds.Contains(t.Id) && t.IsActive)
            .Select(t => new { t.Id, t.Name, t.TimeZoneId })
            .ToListAsync(ct);

        var tenantMap = tenants.ToDictionary(t => t.Id);

        // Dev fallback: Finbuckle uses "localhost" as identifier (not a Guid), so all entities
        // are saved with TenantId = Guid.Empty. Load the first active tenant for clinic name.
        (string Name, string TimeZoneId)? devFallback = null;
        if (configs.Any(c => c.TenantId == Guid.Empty))
        {
            var first = await db.Tenants
                .Where(t => t.IsActive)
                .Select(t => new { t.Name, t.TimeZoneId })
                .FirstOrDefaultAsync(ct);
            if (first is not null)
                devFallback = (first.Name, first.TimeZoneId);
        }

        foreach (var config in configs)
        {
            string clinicName;
            string timeZoneId;

            if (config.TenantId == Guid.Empty)
            {
                if (devFallback is null) continue;
                clinicName = devFallback.Value.Name;
                timeZoneId = devFallback.Value.TimeZoneId;
            }
            else
            {
                if (!tenantMap.TryGetValue(config.TenantId, out var tenant)) continue;
                clinicName = tenant.Name;
                timeZoneId = tenant.TimeZoneId;
            }

            await ProcessReminderSlot(config, clinicName, timeZoneId, 1, config.Reminder1HoursBefore, now, ct);

            if (config.Reminder2HoursBefore.HasValue)
                await ProcessReminderSlot(config, clinicName, timeZoneId, 2, config.Reminder2HoursBefore, now, ct);
        }
    }

    private async Task ProcessReminderSlot(
        TenantNotificationConfig config,
        string clinicName,
        string timeZoneId,
        byte slot,
        int? hoursBefore,
        DateTime now,
        CancellationToken ct)
    {
        if (!hoursBefore.HasValue)
            return;

        // Find appointments whose StartAt falls in [now+hoursBefore-20min, now+hoursBefore+15min).
        // Job runs every 15 minutes; the 20-minute lookback gives tolerance for late-firing jobs.
        var reminderWindowStart = now.AddHours(hoursBefore.Value).AddMinutes(-20);
        var reminderWindowEnd = now.AddHours(hoursBefore.Value).AddMinutes(15);

        var appointments = await db.Appointments
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == config.TenantId &&
                        !a.IsDeleted &&
                        a.Status == AppointmentStatus.Scheduled &&
                        a.StartAt >= reminderWindowStart &&
                        a.StartAt < reminderWindowEnd)
            .Select(a => new { a.Id, a.PatientId, a.StartAt })
            .ToListAsync(ct);

        if (appointments.Count == 0)
            return;

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();

        var patients = await db.Patients
            .IgnoreQueryFilters()
            .Where(p => patientIds.Contains(p.Id) &&
                        p.TenantId == config.TenantId &&
                        !p.IsDeleted &&
                        p.SmsOptIn &&
                        p.PhoneMobile != null)
            .Select(p => new { p.Id, p.FirstName, p.PhoneMobile })
            .ToListAsync(ct);

        var patientMap = patients.ToDictionary(p => p.Id);

        foreach (var appt in appointments)
        {
            if (!patientMap.TryGetValue(appt.PatientId, out var patient))
                continue;

            var alreadySent = await logRepository.ExistsAsync(appt.Id, slot, ct);
            if (alreadySent)
                continue;

            var messageBody = BuildMessage(patient.FirstName, clinicName, appt.StartAt, timeZoneId);

            var log = NotificationLog.Create(
                appt.Id,
                appt.PatientId,
                NotificationChannel.Sms,
                slot,
                patient.PhoneMobile!,
                messageBody);

            log.SetTenant(config.TenantId);
            await logRepository.AddAsync(log, ct);

            var result = await channel.SendAsync(patient.PhoneMobile!, messageBody, ct);

            result.Switch(
                providerMessageId =>
                {
                    log.MarkSent(providerMessageId);
                    logger.LogInformation(
                        "Reminder slot {Slot} sent for appointment {AppointmentId} to {Phone}",
                        slot, appt.Id, patient.PhoneMobile);
                },
                errors =>
                {
                    log.MarkFailed(errors.First().Description);
                    logger.LogWarning(
                        "Reminder slot {Slot} failed for appointment {AppointmentId}: {Error}",
                        slot, appt.Id, errors.First().Description);
                });

            await logRepository.UpdateAsync(log, ct);
        }
    }

    private static string BuildMessage(string firstName, string clinicName, DateTime startAtUtc, string timeZoneId)
    {
        DateTime local;
        try
        {
            var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
            local = TimeZoneInfo.ConvertTimeFromUtc(startAtUtc, tz);
        }
        catch
        {
            local = startAtUtc;
        }

        return $"Hi {firstName}, this is a reminder that you have an appointment at {clinicName} on " +
               $"{local:dddd, MMMM d} at {local:h:mm tt}. Reply STOP to unsubscribe.";
    }
}
