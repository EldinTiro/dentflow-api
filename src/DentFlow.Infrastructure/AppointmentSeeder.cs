using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Appointments.Domain;
using DentFlow.Patients.Domain;
using DentFlow.Staff.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure;

public static class AppointmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        var allTenants = await tenantStore.GetAllAsync();

        foreach (var tenant in allTenants)
        {
            // Id is the tenant's Guid; Identifier is the slug.
            if (!Guid.TryParse(tenant.Id, out var tenantId)) continue;

            var existing = await db.Set<Appointment>()
                .IgnoreQueryFilters()
                .CountAsync(a => a.TenantId == tenantId);

            if (existing >= 50)
            {
                logger.LogDebug("Appointment seed already present for tenant {TenantName} ({Count} appointments)", tenant.Name, existing);
                continue;
            }

            logger.LogInformation("Seeding appointments for tenant {TenantName}", tenant.Name);

            // Load supporting data
            var providers = await db.Set<StaffMember>()
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.IsActive &&
                            (s.StaffType == StaffType.Dentist || s.StaffType == StaffType.Hygienist))
                .ToListAsync();

            var apptTypes = await db.Set<AppointmentType>()
                .IgnoreQueryFilters()
                .Where(t => t.TenantId == tenantId)
                .ToListAsync();

            var patients = await db.Set<Patient>()
                .IgnoreQueryFilters()
                .Where(p => p.TenantId == tenantId && p.Status == PatientStatus.Active)
                .ToListAsync();

            if (providers.Count == 0 || apptTypes.Count == 0 || patients.Count == 0)
            {
                logger.LogWarning("Missing prerequisite data for appointment seeding on tenant {TenantName}", tenant.Name);
                continue;
            }

            var rng = new Random(72);
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddMonths(-4);
            var endDate   = today.AddDays(28);

            // Track booked slots per provider per day to avoid overlaps
            var bookedSlots = new Dictionary<(Guid providerId, DateTime day), List<(DateTime start, DateTime end)>>();

            int totalSeeded = 0;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                bool isPast   = date < today;
                bool isToday  = date.Date == today;
                bool isFuture = date > today;

                // Each provider gets 2-4 appointments per day (past) or 2-3 (future)
                int maxPerProvider = isPast ? rng.Next(2, 5) : rng.Next(1, 4);

                foreach (var provider in providers)
                {
                    var dayKey = (provider.Id, date);
                    bookedSlots.TryAdd(dayKey, new List<(DateTime, DateTime)>());
                    var slots = bookedSlots[dayKey];

                    // Hygienists do more cleaning/routine types; dentists do variety
                    var typePool = apptTypes;

                    for (int i = 0; i < maxPerProvider; i++)
                    {
                        var apptType = typePool[rng.Next(typePool.Count)];
                        var durationMin = apptType.DefaultDurationMinutes;

                        // Find a free slot between 8:00 and 17:00
                        DateTime? slotStart = FindFreeSlot(date, durationMin, slots, rng);
                        if (slotStart is null) break;

                        var slotEnd = slotStart.Value.AddMinutes(durationMin);
                        slots.Add((slotStart.Value, slotEnd));

                        var patient = patients[rng.Next(patients.Count)];
                        bool isNewPt = patient.FirstVisitDate is null;

                        var appt = Appointment.Create(
                            patient.Id,
                            provider.Id,
                            apptType.Id,
                            slotStart.Value,
                            slotEnd,
                            chiefComplaint: rng.Next(4) == 0 ? "Tooth pain" : null,
                            isNewPatient: isNewPt);

                        appt.SetTenant(tenantId);

                        if (isPast)
                        {
                            int roll = rng.Next(100);
                            if (roll < 72)
                            {
                                appt.CheckIn();
                                appt.Start();
                                appt.Complete();
                                var visitDate = DateOnly.FromDateTime(date);
                                patient.UpdateLastVisit(visitDate);
                                // Set recall 6 months from last visit
                                patient.SetRecall(visitDate.AddMonths(6));
                            }
                            else if (roll < 87)
                            {
                                appt.Cancel("Patient cancelled", null);
                            }
                            else
                            {
                                appt.MarkNoShow();
                            }
                        }
                        else if (isToday)
                        {
                            int roll = rng.Next(100);
                            if (slotStart.Value.Hour < DateTime.UtcNow.Hour)
                            {
                                if (roll < 60)
                                {
                                    appt.CheckIn();
                                    appt.Start();
                                    appt.Complete();
                                }
                                else
                                {
                                    appt.CheckIn();
                                    appt.Start();
                                }
                            }
                            // else: stay Scheduled
                        }
                        // future: stay Scheduled

                        db.Set<Appointment>().Add(appt);
                        totalSeeded++;
                    }
                }
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} appointments for tenant {TenantName}", totalSeeded, tenant.Name);
        }
    }

    private static DateTime? FindFreeSlot(
        DateTime day,
        int durationMin,
        List<(DateTime start, DateTime end)> booked,
        Random rng)
    {
        // Try up to 20 random start times in [08:00, 17:00 - duration]
        int workStart = 8 * 60;   // minutes from midnight
        int workEnd   = 17 * 60;
        int latest    = workEnd - durationMin;

        if (latest <= workStart) return null;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Pick a slot aligned to 15-minute boundaries
            int rangeMinutes = latest - workStart;
            int offsetBlocks = rng.Next(rangeMinutes / 15);
            int startMinutes = workStart + offsetBlocks * 15;

            var candidate = day.AddMinutes(startMinutes);
            var candidateEnd = candidate.AddMinutes(durationMin);

            bool overlaps = booked.Any(b =>
                candidate < b.end && candidateEnd > b.start);

            if (!overlaps)
                return candidate;
        }

        return null;
    }
}
