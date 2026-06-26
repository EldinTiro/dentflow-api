using FastEndpoints;
using DentFlow.Notifications.Application.Interfaces;
using DentFlow.Notifications.Domain;

namespace DentFlow.Notifications.Endpoints;

public class TwilioStatusCallbackRequest
{
    public string MessageSid { get; init; } = string.Empty;
    public string MessageStatus { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Receives Twilio delivery status webhooks. No auth — validated by Twilio request signature.
/// Configure this URL in your Twilio console: POST /api/v1/webhooks/twilio/sms-status
/// </summary>
public class TwilioStatusCallbackEndpoint(INotificationLogRepository logRepository)
    : Endpoint<TwilioStatusCallbackRequest>
{
    public override void Configure()
    {
        Post("/webhooks/twilio/sms-status");
        AllowAnonymous();
        Version(1);
        Summary(s => s.Summary = "Twilio SMS delivery status callback");
    }

    public override async Task HandleAsync(TwilioStatusCallbackRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.MessageSid))
        {
            await SendOkAsync(ct);
            return;
        }

        var log = await logRepository.GetByProviderMessageIdAsync(req.MessageSid, ct);
        if (log is null)
        {
            await SendOkAsync(ct);
            return;
        }

        switch (req.MessageStatus.ToLowerInvariant())
        {
            case "delivered":
                log.MarkDelivered();
                break;
            case "failed":
            case "undelivered":
                log.MarkFailed(req.ErrorMessage ?? req.ErrorCode ?? req.MessageStatus);
                break;
        }

        await logRepository.UpdateAsync(log, ct);
        await SendOkAsync(ct);
    }
}
