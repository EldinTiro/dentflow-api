using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Appointments.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure;

public static class AppointmentTypeSeeder
{
    private static readonly (string Name, int Duration, string Color, string? Description)[] Defaults =
    [
        ("Routine Checkup",    30, "#3B82F6", "Standard dental checkup and cleaning"),
        ("New Patient Exam",   60, "#8B5CF6", "Comprehensive exam for new patients"),
        ("Emergency Visit",    30, "#EF4444", "Urgent dental care"),
        ("Teeth Cleaning",     45, "#10B981", "Professional cleaning (prophylaxis)"),
        ("Filling",            45, "#F59E0B", "Composite or amalgam restorations"),
        ("Crown / Bridge",     90, "#6366F1", "Crown preparation or bridge work"),
        ("Root Canal",         90, "#EC4899", "Endodontic treatment"),
        ("Extraction",         30, "#F97316", "Tooth extraction"),
        ("Orthodontic Consult",30, "#14B8A6", "Initial orthodontic evaluation"),
        ("Whitening",          60, "#FCD34D", "In-office tooth whitening"),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Use Finbuckle's store — tenants live in config/DB there, not in db.Tenants
        var allTenants = await tenantStore.GetAllAsync();

        foreach (var tenant in allTenants)
        {
            // In dev Identifier = "localhost" (not a Guid) → Guid.Empty matches dev entities.
            // In production Identifier = the tenant's Guid string.
            // Id is the tenant's Guid; Identifier is the slug.
            if (!Guid.TryParse(tenant.Id, out var tenantId)) continue;

            var hasTypes = await db.AppointmentTypes
                .IgnoreQueryFilters()
                .AnyAsync(t => t.TenantId == tenantId && !t.IsDeleted);

            if (hasTypes)
            {
                logger.LogDebug("Appointment types already seeded for tenant {TenantName}", tenant.Name);
                continue;
            }

            foreach (var (name, duration, color, description) in Defaults)
            {
                var type = AppointmentType.Create(name, duration, description, color);
                type.SetTenant(tenantId);
                db.AppointmentTypes.Add(type);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} appointment types for tenant {TenantName}", Defaults.Length, tenant.Name);
        }
    }
}
