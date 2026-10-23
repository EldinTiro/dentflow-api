namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Appointment facts other modules need (people-merge plan B2/B4/B6) without a
/// reference to the Appointments module. Tenant-scoped like any other read.
/// </summary>
public interface IAppointmentReader
{
    /// <summary>
    /// Appointments still ahead for this patient: not cancelled, completed or no-show,
    /// and not over yet at <paramref name="now"/>.
    /// </summary>
    Task<int> CountUpcomingForPatientAsync(Guid patientId, DateTime now, CancellationToken ct);
}
