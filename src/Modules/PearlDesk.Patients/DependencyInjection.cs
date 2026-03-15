﻿using Microsoft.Extensions.DependencyInjection;

namespace PearlDesk.Patients;

/// <summary>
/// Patient module services are registered via PearlDesk.Infrastructure.DependencyInjection.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services) => services;
}

