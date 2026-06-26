using Microsoft.Extensions.DependencyInjection;

namespace DentFlow.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services) => services;
}
