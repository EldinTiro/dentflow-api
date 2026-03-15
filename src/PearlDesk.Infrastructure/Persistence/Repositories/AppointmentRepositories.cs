using Microsoft.EntityFrameworkCore;
using PearlDesk.Appointments.Application.Interfaces;
using PearlDesk.Appointments.Domain;
using PearlDesk.Infrastructure.Persistence;

namespace PearlDesk.Infrastructure.Persistence.Repositories;

public class AppointmentRepository(ApplicationDbContext dbContext) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Appointment>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<bool> HasProviderConflictAsync(
        Guid providerId, DateTime startAt, DateTime endAt, Guid? excludeAppointmentId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Appointment>()
            .Where(a => a.ProviderId == providerId
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow
                && a.StartAt < endAt
                && a.EndAt > startAt);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
        Guid? patientId, Guid? providerId, DateOnly? dateFrom, DateOnly? dateTo,
        string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Appointment>().AsQueryable();

        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId.Value);
        if (providerId.HasValue) query = query.Where(a => a.ProviderId == providerId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);

        if (dateFrom.HasValue)
        {
            var fromDt = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartAt >= fromDt);
        }
        if (dateTo.HasValue)
        {
            var toDt = dateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.StartAt <= toDt);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(a => a.StartAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<Appointment>().AddAsync(appointment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Appointment>().Update(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        appointment.SoftDelete();
        dbContext.Set<Appointment>().Update(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class AppointmentTypeRepository(ApplicationDbContext dbContext) : IAppointmentTypeRepository
{
    public async Task<AppointmentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<AppointmentType>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AppointmentType>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Set<AppointmentType>().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(AppointmentType appointmentType, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<AppointmentType>().AddAsync(appointmentType, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AppointmentType appointmentType, CancellationToken cancellationToken = default)
    {
        dbContext.Set<AppointmentType>().Update(appointmentType);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(AppointmentType appointmentType, CancellationToken cancellationToken = default)
    {
        appointmentType.SoftDelete();
        dbContext.Set<AppointmentType>().Update(appointmentType);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

