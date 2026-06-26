using ErrorOr;
using MediatR;
using DentFlow.Notifications.Application.Interfaces;
using DentFlow.Notifications.Domain;

namespace DentFlow.Notifications.Application.Queries;

public record GetNotificationConfigQuery(Guid TenantId) : IRequest<ErrorOr<NotificationConfigResponse>>;

public class GetNotificationConfigQueryHandler(ITenantNotificationConfigRepository configRepository)
    : IRequestHandler<GetNotificationConfigQuery, ErrorOr<NotificationConfigResponse>>
{
    public async Task<ErrorOr<NotificationConfigResponse>> Handle(
        GetNotificationConfigQuery query,
        CancellationToken ct)
    {
        var config = await configRepository.GetByTenantIdAsync(query.TenantId, ct);

        return config is null
            ? NotificationConfigResponse.Default()
            : NotificationConfigResponse.FromEntity(config);
    }
}
