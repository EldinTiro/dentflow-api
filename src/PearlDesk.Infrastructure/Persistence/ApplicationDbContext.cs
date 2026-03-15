﻿using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PearlDesk.Appointments.Domain;
using PearlDesk.Domain.Common;
using PearlDesk.Domain.Identity;
using PearlDesk.Domain.Tenants;
using PearlDesk.Patients.Domain;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IMultiTenantContextAccessor multiTenantContextAccessor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    // Tenant & Identity
    public DbSet<Domain.Tenants.Tenant> Tenants => Set<Domain.Tenants.Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Staff
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffAvailability> StaffAvailabilities => Set<StaffAvailability>();
    public DbSet<StaffBlockedTime> StaffBlockedTimes => Set<StaffBlockedTime>();

    // Patients
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientEmergencyContact> PatientEmergencyContacts => Set<PatientEmergencyContact>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();

    // Appointments
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();

    private Guid? CurrentTenantId =>
        Guid.TryParse(multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier, out var id)
            ? id
            : null;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables to snake_case
        builder.Entity<ApplicationUser>().ToTable("asp_net_users");
        builder.Entity<ApplicationRole>().ToTable("asp_net_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("asp_net_user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("asp_net_user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("asp_net_user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("asp_net_role_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("asp_net_user_tokens");

        // Tenant (not tenant-scoped itself)
        builder.Entity<Domain.Tenants.Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Slug).HasMaxLength(63).IsRequired();
            e.Property(t => t.Name).HasMaxLength(255).IsRequired();
            e.Property(t => t.Plan).HasMaxLength(50);
            e.Property(t => t.StripeCustomerId).HasMaxLength(255);
            e.Property(t => t.StripeSubscriptionId).HasMaxLength(255);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(r => r.Id);
            e.Property(r => r.TokenHash).HasMaxLength(500).IsRequired();
            e.Property(r => r.ReplacedByTokenHash).HasMaxLength(500);
            e.Property(r => r.CreatedByIp).HasMaxLength(50);
            e.Property(r => r.UserAgent).HasMaxLength(500);
            e.HasIndex(r => r.TokenHash);
            e.HasIndex(r => new { r.UserId, r.FamilyId });
        });

        // Apply all EF entity configurations from this assembly (Infrastructure)
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Apply Global Query Filters to all TenantAuditableEntity types
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(TenantAuditableEntity).IsAssignableFrom(entityType.ClrType)) continue;

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildTenantSoftDeleteFilter(entityType.ClrType));
        }
    }

    private System.Linq.Expressions.LambdaExpression BuildTenantSoftDeleteFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");

        var tenantIdProp = System.Linq.Expressions.Expression.Property(param, nameof(TenantAuditableEntity.TenantId));
        var currentTenantId = System.Linq.Expressions.Expression.Constant(CurrentTenantId ?? Guid.Empty);
        var tenantFilter = System.Linq.Expressions.Expression.Equal(tenantIdProp, currentTenantId);

        var isDeletedProp = System.Linq.Expressions.Expression.Property(param, nameof(TenantAuditableEntity.IsDeleted));
        var notDeleted = System.Linq.Expressions.Expression.Not(isDeletedProp);

        var combined = System.Linq.Expressions.Expression.AndAlso(tenantFilter, notDeleted);

        return System.Linq.Expressions.Expression.Lambda(combined, param);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var tenantId = CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<TenantAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (tenantId.HasValue)
                    entry.Entity.SetTenant(tenantId.Value);

                // CreatedAt is set in the entity constructor
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdated();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

