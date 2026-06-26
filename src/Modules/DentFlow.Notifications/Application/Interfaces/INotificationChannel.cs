using ErrorOr;

namespace DentFlow.Notifications.Application.Interfaces;

public interface INotificationChannel
{
    Task<ErrorOr<string>> SendAsync(string toPhoneNumber, string message, CancellationToken ct);
}
