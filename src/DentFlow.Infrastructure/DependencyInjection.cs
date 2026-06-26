using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Infrastructure.Persistence.Repositories;
using DentFlow.Infrastructure.Services;
using DentFlow.Notifications.Application.Interfaces;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Tenants.Application.Interfaces;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Treatments.Application.Interfaces;

namespace DentFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        var redisConnection = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = redisConnection);
        }
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.AddScoped<IStorageService, S3StorageService>();

        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientDocumentRepository, PatientDocumentRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IAppointmentTypeRepository, AppointmentTypeRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IProviderBlockedTimeChecker, ProviderBlockedTimeChecker>();
        services.AddScoped<IProviderAvailabilityReader, ProviderAvailabilityReader>();
        services.AddScoped<ITreatmentPlanReader, TreatmentPlanReader>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IFeatureService, TierFeatureService>();

        // Notifications
        services.Configure<TwilioSettings>(configuration.GetSection("Twilio"));
        services.Configure<NotificationSettings>(configuration.GetSection("Notifications"));

        var notificationSettings = configuration.GetSection("Notifications").Get<NotificationSettings>();
        if (environment.IsDevelopment() || (notificationSettings?.UseFakeChannel ?? false))
            services.AddScoped<INotificationChannel, FakeSmsChannel>();
        else
            services.AddScoped<INotificationChannel, SmsChannel>();

        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<ITenantNotificationConfigRepository, TenantNotificationConfigRepository>();

        // Hangfire jobs
        services.AddScoped<AppointmentReminderJob>();

        return services;
    }
}
