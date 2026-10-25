using Microsoft.EntityFrameworkCore;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

internal sealed class AppointmentReader(ApplicationDbContext db) : IAppointmentReader
{
    private static readonly string[] Open = [AppointmentStatus.Scheduled, AppointmentStatus.CheckedIn, AppointmentStatus.InProgress];

    public Task<int> CountUpcomingForPatientAsync(Guid patientId, DateTime now, CancellationToken ct) =>
        db.Appointments.CountAsync(a => a.PatientId == patientId && Open.Contains(a.Status) && a.EndAt > now, ct);

    public Task<int> CountUpcomingForProviderAsync(Guid providerId, DateTime now, CancellationToken ct) =>
        db.Appointments.CountAsync(a => a.ProviderId == providerId && Open.Contains(a.Status) && a.EndAt > now, ct);

    public Task<bool> ProviderHasAnyAsync(Guid providerId, CancellationToken ct) =>
        db.Appointments.AnyAsync(a => a.ProviderId == providerId, ct);
}
