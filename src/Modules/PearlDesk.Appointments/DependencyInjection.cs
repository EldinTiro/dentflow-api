﻿using Microsoft.Extensions.DependencyInjection;

namespace PearlDesk.Appointments;

/// <summary>
/// Appointment module services are registered via PearlDesk.Infrastructure.DependencyInjection.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAppointmentsModule(this IServiceCollection services) => services;
}

