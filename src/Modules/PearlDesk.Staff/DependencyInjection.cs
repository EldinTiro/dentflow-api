using Microsoft.Extensions.DependencyInjection;

namespace PearlDesk.Staff;

/// <summary>
/// Staff module services are registered via PearlDesk.Infrastructure.DependencyInjection.
/// This class is kept as a placeholder for any future module-specific registrations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddStaffModule(this IServiceCollection services) => services;
}

