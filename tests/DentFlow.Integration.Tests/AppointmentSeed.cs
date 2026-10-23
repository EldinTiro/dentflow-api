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

        var appointment = Appointment.Create(patientId, providerId ?? Guid.NewGuid(), Guid.NewGuid(), startAt, startAt.AddMinutes(30));
        appointment.SetTenant(patient.TenantId);
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment.Id;
    }
}
