﻿﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Infrastructure.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staff_members");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StaffType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.LastName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(255);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.LicenseNumber).HasMaxLength(100);
        builder.Property(s => s.NpiNumber).HasMaxLength(10);
        builder.Property(s => s.Specialty).HasMaxLength(100);
        builder.Property(s => s.ColorHex).HasColumnType("char(7)");

        builder.HasIndex(s => new { s.TenantId, s.Email }).IsUnique()
            .HasFilter("\"Email\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasMany(s => s.Availabilities)
            .WithOne(a => a.StaffMember)
            .HasForeignKey(a => a.StaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.BlockedTimes)
            .WithOne(b => b.StaffMember)
            .HasForeignKey(b => b.StaffMemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffAvailabilityConfiguration : IEntityTypeConfiguration<StaffAvailability>
{
    public void Configure(EntityTypeBuilder<StaffAvailability> builder)
    {
        builder.ToTable("staff_availability");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DayOfWeek)
            .HasColumnName("day_of_week")
            .HasConversion<short>();

        builder.Property(a => a.StartTime).HasColumnName("start_time");
        builder.Property(a => a.EndTime).HasColumnName("end_time");
        builder.Property(a => a.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(a => a.EffectiveTo).HasColumnName("effective_to");
    }
}

public class StaffBlockedTimeConfiguration : IEntityTypeConfiguration<StaffBlockedTime>
{
    public void Configure(EntityTypeBuilder<StaffBlockedTime> builder)
    {
        builder.ToTable("staff_blocked_times");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.StartAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.EndAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(b => b.Reason).HasMaxLength(255);
    }
}

