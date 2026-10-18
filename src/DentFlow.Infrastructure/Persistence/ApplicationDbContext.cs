﻿using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DentFlow.Appointments.Domain;
using DentFlow.Domain.Common;
using DentFlow.Domain.Identity;
using DentFlow.Domain.Tenants;
using DentFlow.Notifications.Domain;
using DentFlow.Patients.Domain;
using DentFlow.Staff.Domain;
using DentFlow.Treatments.Domain;
using DentFlow.Billing.Domain;

namespace DentFlow.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IMultiTenantContextAccessor multiTenantContextAccessor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    // Tenant & Identity
    public DbSet<Domain.Tenants.Tenant> Tenants => Set<Domain.Tenants.Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    // Staff
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffAvailability> StaffAvailabilities => Set<StaffAvailability>();
    public DbSet<StaffBlockedTime> StaffBlockedTimes => Set<StaffBlockedTime>();

    // Patients
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientEmergencyContact> PatientEmergencyContacts => Set<PatientEmergencyContact>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();

    // Appointments
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistories => Set<AppointmentStatusHistory>();

    // Treatments
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();

    // Billing
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();

    // Notifications
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<TenantNotificationConfig> TenantNotificationConfigs => Set<TenantNotificationConfig>();

    /// <summary>
    /// The resolved tenant, or null when nothing resolved. Exposed for the few places
    /// that must scope a query explicitly — e.g. generating a per-tenant sequence
    /// number, which has to count soft-deleted rows and therefore cannot rely on the
    /// global query filter.
    /// </summary>
    public Guid? ResolvedTenantId => CurrentTenantId;

    // Id carries the tenant's Guid; Identifier carries the slug. Parsing Identifier
    // never succeeded, so every request resolved to Guid.Empty and tenants shared data.
    private Guid? CurrentTenantId =>
        Guid.TryParse(multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id, out var id)
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
            e.Property(t => t.WorkdayStart).HasColumnName("WorkdayStart").HasColumnType("time").HasDefaultValue(new TimeOnly(8, 0));
            e.Property(t => t.WorkdayEnd).HasColumnName("WorkdayEnd").HasColumnType("time").HasDefaultValue(new TimeOnly(22, 30));
            e.Property(t => t.SlotDurationMinutes).HasColumnName("SlotDurationMinutes").HasDefaultValue(30);
            e.Property(t => t.WeeklyScheduleJson).HasColumnName("WeeklyScheduleJson").HasColumnType("text");
            e.Property(t => t.TimeZoneId).HasMaxLength(100).HasDefaultValue("UTC");
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

    /// <summary>
    /// Read by the global query filters. Must be a property on the context, not a
    /// captured value: <see cref="OnModelCreating"/> runs once and the model is cached,
    /// so a baked-in constant would pin every later request to whichever tenant was
    /// current when the model was first built — in practice Guid.Empty during startup
    /// migration, which made filtered reads invisible to every real tenant.
    /// </summary>
    public Guid FilterTenantId => CurrentTenantId ?? Guid.Empty;

    private System.Linq.Expressions.LambdaExpression BuildTenantSoftDeleteFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");

        var tenantIdProp = System.Linq.Expressions.Expression.Property(param, nameof(TenantAuditableEntity.TenantId));
        // Referencing the context instance makes EF re-evaluate this per query.
        var currentTenantId = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Constant(this), nameof(FilterTenantId));
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
                {
                    // Resolved context always wins, so a handler cannot write elsewhere.
                    entry.Entity.SetTenant(tenantId.Value);
                }
                else if (entry.Entity.TenantId == Guid.Empty)
                {
                    // Fail closed. Falling back to Guid.Empty here is what orphaned
                    // every existing row into a shared pool.
                    throw new InvalidOperationException(
                        $"Cannot persist {entry.Entity.GetType().Name}: no tenant resolved and no tenant set " +
                        "explicitly. Seeders and background jobs must call SetTenant before saving.");
                }
                // Otherwise the caller set the tenant explicitly (seeders, jobs) — keep it.
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdated();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

