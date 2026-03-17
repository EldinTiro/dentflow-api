using DentFlow.Application.Common.Interfaces;
using DentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Queries the StaffBlockedTimes table to determine whether a provider has an active
/// blocked period overlapping the requested appointment window.
/// </summary>
public class ProviderBlockedTimeChecker(ApplicationDbContext dbContext) : IProviderBlockedTimeChecker
{
    public Task<bool> IsProviderBlockedAsync(
        Guid providerId,
        DateTime startAt,
        DateTime endAt,
        CancellationToken cancellationToken = default)
    {
        // Overlap condition: blocked.StartAt < endAt AND blocked.EndAt > startAt
        return dbContext.StaffBlockedTimes
            .AnyAsync(
                b => b.StaffMemberId == providerId
                     && b.StartAt < endAt
                     && b.EndAt > startAt,
                cancellationToken);
    }
}
