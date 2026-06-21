using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Domain;
using DentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Computes free appointment slots for a provider on a given date.
/// Considers staff availability schedule, blocked times, and existing appointments.
/// </summary>
public class ProviderAvailabilityReader(ApplicationDbContext db) : IProviderAvailabilityReader
{
    private const int SlotStepMinutes = 15;

    public async Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
        Guid providerId,
        DateOnly date,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        var dayOfWeek = (short)date.DayOfWeek;
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // 1. Fetch provider availability for this day of week
        var availabilities = await db.StaffAvailabilities
            .Where(a => a.StaffMemberId == providerId
                && a.DayOfWeek == (DayOfWeek)dayOfWeek
                && a.EffectiveFrom <= date
                && (a.EffectiveTo == null || a.EffectiveTo.Value >= date))
            .ToListAsync(cancellationToken);

        if (availabilities.Count == 0)
            return [];

        // 2. Fetch existing appointments (non-terminal) for that provider on that day
        var busyAppointments = await db.Appointments
            .Where(a => a.ProviderId == providerId
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.StartAt >= dayStart
                && a.StartAt <= dayEnd)
            .Select(a => new { a.StartAt, a.EndAt })
            .ToListAsync(cancellationToken);

        // 3. Fetch blocked times for that day
        var blockedTimes = await db.StaffBlockedTimes
            .Where(b => b.StaffMemberId == providerId
                && b.StartAt < dayEnd
                && b.EndAt > dayStart)
            .Select(b => new { b.StartAt, b.EndAt })
            .ToListAsync(cancellationToken);

        var slots = new List<TimeSlot>();

        foreach (var availability in availabilities)
        {
            var workStart = date.ToDateTime(availability.StartTime, DateTimeKind.Utc);
            var workEnd = date.ToDateTime(availability.EndTime, DateTimeKind.Utc);

            var slotStart = workStart;
            while (slotStart.AddMinutes(durationMinutes) <= workEnd)
            {
                var slotEnd = slotStart.AddMinutes(durationMinutes);

                // Check overlap with existing appointments
                var hasAppointmentConflict = busyAppointments.Any(a =>
                    a.StartAt < slotEnd && a.EndAt > slotStart);

                // Check overlap with blocked times
                var hasBlockedConflict = blockedTimes.Any(b =>
                    b.StartAt < slotEnd && b.EndAt > slotStart);

                if (!hasAppointmentConflict && !hasBlockedConflict)
                    slots.Add(new TimeSlot(slotStart, slotEnd));

                slotStart = slotStart.AddMinutes(SlotStepMinutes);
            }
        }

        return slots.OrderBy(s => s.StartAt).ToList().AsReadOnly();
    }
}
