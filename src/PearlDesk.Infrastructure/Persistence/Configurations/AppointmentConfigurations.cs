﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PearlDesk.Appointments.Domain;

namespace PearlDesk.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasMaxLength(30).IsRequired();
        builder.Property(a => a.StartAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(a => a.EndAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(a => a.CancelledAt).HasColumnType("timestamptz");
        builder.Property(a => a.ConfirmedAt).HasColumnType("timestamptz");
        builder.Property(a => a.CheckedInAt).HasColumnType("timestamptz");
        builder.Property(a => a.CompletedAt).HasColumnType("timestamptz");
        builder.Property(a => a.NoShowAt).HasColumnType("timestamptz");
        builder.Property(a => a.ReminderSentAt).HasColumnType("timestamptz");
        builder.Property(a => a.CancellationReason).HasMaxLength(255);
        builder.Property(a => a.Source).HasMaxLength(30).IsRequired();
        builder.Property(a => a.ColorHex).HasColumnType("char(7)");

        builder.HasIndex(a => new { a.TenantId, a.ProviderId, a.StartAt });
        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.StartAt });
    }
}

public class AppointmentTypeConfiguration : IEntityTypeConfiguration<AppointmentType>
{
    public void Configure(EntityTypeBuilder<AppointmentType> builder)
    {
        builder.ToTable("appointment_types");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(255).IsRequired();
        builder.Property(t => t.ColorHex).HasColumnType("char(7)");
        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

