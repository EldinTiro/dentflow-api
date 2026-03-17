namespace DentFlow.Application.Common.Interfaces;

/// <summary>
/// Cross-module service that checks whether a provider (staff member) has a blocked
/// time entry overlapping a given appointment window.
/// Defined here so the Appointments module can consume it without referencing the Staff module directly.
/// Implementation lives in DentFlow.Infrastructure.
/// </summary>
public interface IProviderBlockedTimeChecker
{
    Task<bool> IsProviderBlockedAsync(
        Guid providerId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default);
}
