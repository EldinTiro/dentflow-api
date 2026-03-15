using PearlDesk.Appointments.Domain;

namespace PearlDesk.Appointments.Application.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasProviderConflictAsync(Guid providerId, DateTime startAt, DateTime endAt, Guid? excludeAppointmentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
        Guid? patientId,
        Guid? providerId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Appointment appointment, CancellationToken cancellationToken = default);
}

public interface IAppointmentTypeRepository
{
    Task<AppointmentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentType>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AppointmentType appointmentType, CancellationToken cancellationToken = default);
    Task UpdateAsync(AppointmentType appointmentType, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(AppointmentType appointmentType, CancellationToken cancellationToken = default);
}

