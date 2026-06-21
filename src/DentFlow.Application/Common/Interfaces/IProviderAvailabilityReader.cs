namespace DentFlow.Application.Common.Interfaces;

public record TimeSlot(DateTime StartAt, DateTime EndAt);

/// <summary>
/// Cross-module service that computes available appointment slots for a provider on a given date.
/// Considers both staff availability schedule and existing appointments/blocked times.
/// Implementation lives in DentFlow.Infrastructure.
/// </summary>
public interface IProviderAvailabilityReader
{
    /// <summary>
    /// Returns a list of free time slots for the provider on the requested date,
    /// each of the requested duration. Excludes times blocked by existing appointments or blocked-time entries.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
        Guid providerId,
        DateOnly date,
        int durationMinutes,
        CancellationToken cancellationToken = default);
}
