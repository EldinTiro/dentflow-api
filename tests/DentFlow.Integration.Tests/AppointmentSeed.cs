using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Appointments.Domain;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Patients.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Puts an appointment straight into the database, in the patient's clinic. The booking
/// API refuses past times and needs a provider schedule, which history tests don't care about.
/// </summary>
internal static class AppointmentSeed
{
    public static async Task<Guid> BookAsync(
        DentFlowAppFactory factory, Guid patientId, DateTime startAt, Guid? providerId = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var patient = await db.Set<Patient>().IgnoreQueryFilters().SingleAsync(p => p.Id == patientId);

        return await AddAsync(db, patient.TenantId, patientId, providerId ?? Guid.NewGuid(), startAt);
    }

    /// <summary>An appointment with this provider in this clinic; the patient doesn't matter.</summary>
    public static async Task<Guid> BookWithProviderAsync(DentFlowAppFactory factory, Guid tenantId, Guid providerId, DateTime startAt)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await AddAsync(db, tenantId, Guid.NewGuid(), providerId, startAt);
    }

    private static async Task<Guid> AddAsync(ApplicationDbContext db, Guid tenantId, Guid patientId, Guid providerId, DateTime startAt)
    {
        var appointment = Appointment.Create(patientId, providerId, Guid.NewGuid(), startAt, startAt.AddMinutes(30));
        appointment.SetTenant(tenantId);
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment.Id;
    }
}
