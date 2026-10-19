using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Staff.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure;

public static class StaffSeeder
{
    private record StaffTemplate(
        StaffType Type,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        DateOnly HireDate,
        string? Specialty,
        string ColorHex);

    private static readonly StaffTemplate[] Templates =
    [
        new(StaffType.Dentist,      "Amira",  "Hadžić",  "amira.hadzic@dentflow.ba",  "+387 61 100 001", new DateOnly(2019, 3, 1),  "General Dentistry",  "#4F46E5"),
        new(StaffType.Dentist,      "Jasmin", "Begić",   "jasmin.begic@dentflow.ba",  "+387 61 100 002", new DateOnly(2020, 7, 15), "Orthodontics",       "#7C3AED"),
        new(StaffType.Hygienist,    "Selma",  "Terzić",  "selma.terzic@dentflow.ba",  "+387 61 100 003", new DateOnly(2021, 1, 10), "Dental Hygiene",     "#10B981"),
        new(StaffType.Hygienist,    "Faruk",  "Delić",   "faruk.delic@dentflow.ba",   "+387 61 100 004", new DateOnly(2021, 6, 1),  "Dental Hygiene",     "#059669"),
        new(StaffType.Receptionist, "Ema",    "Babić",   "ema.babic@dentflow.ba",     "+387 61 100 005", new DateOnly(2022, 2, 14), null,                 "#F59E0B"),
        new(StaffType.OfficeManager,"Tarik",  "Softić",  "tarik.softic@dentflow.ba",  "+387 61 100 006", new DateOnly(2018, 9, 3),  null,                 "#6B7280"),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        var allTenants = await tenantStore.GetAllAsync();

        foreach (var tenant in allTenants)
        {
            // Id is the tenant's Guid; Identifier is the slug.
            if (!Guid.TryParse(tenant.Id, out var tenantId)) continue;

            var existing = await db.Set<StaffMember>()
                .IgnoreQueryFilters()
                .CountAsync(s => s.TenantId == tenantId);

            if (existing >= 6)
            {
                logger.LogDebug("Staff seed already present for tenant {TenantName} ({Count} staff)", tenant.Name, existing);
                continue;
            }

            logger.LogInformation("Seeding staff for tenant {TenantName}", tenant.Name);

            foreach (var tmpl in Templates)
            {
                var member = StaffMember.Create(
                    tmpl.Type,
                    tmpl.FirstName,
                    tmpl.LastName,
                    tmpl.Email,
                    tmpl.Phone,
                    tmpl.HireDate,
                    tmpl.Specialty,
                    tmpl.ColorHex);

                member.SetTenant(tenantId);
                db.Set<StaffMember>().Add(member);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} staff members for tenant {TenantName}", Templates.Length, tenant.Name);
        }
    }
}
