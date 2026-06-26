using ErrorOr;
using Microsoft.Extensions.Logging;
using DentFlow.Notifications.Application.Interfaces;

namespace DentFlow.Infrastructure.Services;

internal sealed class FakeSmsChannel(ILogger<FakeSmsChannel> logger) : INotificationChannel
{
    public Task<ErrorOr<string>> SendAsync(string toPhoneNumber, string message, CancellationToken ct)
    {
        var fakeMessageId = $"FAKE_{Guid.NewGuid():N}";
        logger.LogInformation(
            "[FakeSms] To={To} MessageId={MessageId} Body={Body}",
            toPhoneNumber, fakeMessageId, message);

        return Task.FromResult<ErrorOr<string>>(fakeMessageId);
    }
}
