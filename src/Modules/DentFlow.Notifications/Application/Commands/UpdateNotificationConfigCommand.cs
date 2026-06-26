using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Features;
using DentFlow.Notifications.Application.Interfaces;
using DentFlow.Notifications.Domain;

namespace DentFlow.Notifications.Application.Commands;

public record UpdateNotificationConfigCommand(
    Guid TenantId,
    bool SmsEnabled,
    int? Reminder1HoursBefore,
    int? Reminder2HoursBefore) : IRequest<ErrorOr<NotificationConfigResponse>>;

public class UpdateNotificationConfigCommandValidator : AbstractValidator<UpdateNotificationConfigCommand>
{
    public UpdateNotificationConfigCommandValidator()
    {
        RuleFor(x => x.Reminder1HoursBefore)
            .InclusiveBetween(1, 168)
            .When(x => x.Reminder1HoursBefore.HasValue)
            .WithMessage("Reminder 1 must be between 1 and 168 hours before the appointment.");

        RuleFor(x => x.Reminder2HoursBefore)
            .InclusiveBetween(1, 168)
            .When(x => x.Reminder2HoursBefore.HasValue)
            .WithMessage("Reminder 2 must be between 1 and 168 hours before the appointment.");

        RuleFor(x => x)
            .Must(x => x.Reminder2HoursBefore is null ||
                        x.Reminder1HoursBefore is null ||
                        x.Reminder2HoursBefore < x.Reminder1HoursBefore)
            .WithMessage("Reminder 2 must be closer to the appointment than Reminder 1.");
    }
}

public class UpdateNotificationConfigCommandHandler(
    ITenantNotificationConfigRepository configRepository,
    IFeatureService featureService)
    : IRequestHandler<UpdateNotificationConfigCommand, ErrorOr<NotificationConfigResponse>>
{
    public async Task<ErrorOr<NotificationConfigResponse>> Handle(
        UpdateNotificationConfigCommand cmd,
        CancellationToken ct)
    {
        if (cmd.SmsEnabled && !featureService.IsEnabled(FeatureFlag.SmsNotifications))
            return NotificationErrors.FeatureDisabled;

        var config = await configRepository.GetByTenantIdAsync(cmd.TenantId, ct);

        if (config is null)
        {
            config = TenantNotificationConfig.CreateDefault(cmd.TenantId);
            config.Update(cmd.SmsEnabled, cmd.Reminder1HoursBefore, cmd.Reminder2HoursBefore);
            await configRepository.AddAsync(config, ct);
        }
        else
        {
            config.Update(cmd.SmsEnabled, cmd.Reminder1HoursBefore, cmd.Reminder2HoursBefore);
            await configRepository.UpdateAsync(config, ct);
        }

        return NotificationConfigResponse.FromEntity(config);
    }
}
