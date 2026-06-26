using ErrorOr;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using DentFlow.Notifications.Application.Interfaces;

namespace DentFlow.Infrastructure.Services;

internal sealed class SmsChannel(IOptions<TwilioSettings> options) : INotificationChannel
{
    private readonly TwilioSettings _settings = options.Value;

    public async Task<ErrorOr<string>> SendAsync(string toPhoneNumber, string message, CancellationToken ct)
    {
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);

        try
        {
            var msg = await MessageResource.CreateAsync(
                to: new Twilio.Types.PhoneNumber(toPhoneNumber),
                from: new Twilio.Types.PhoneNumber(_settings.FromNumber),
                body: message);

            return msg.Sid;
        }
        catch (Exception ex)
        {
            return Error.Failure("Sms.SendFailed", ex.Message);
        }
    }
}
