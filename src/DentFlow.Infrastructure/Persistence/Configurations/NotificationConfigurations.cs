using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DentFlow.Notifications.Domain;

namespace DentFlow.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.ToPhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(n => n.MessageBody).HasColumnType("text").IsRequired();
        builder.Property(n => n.ProviderMessageId).HasMaxLength(100);
        builder.Property(n => n.FailureReason).HasMaxLength(500);
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.DeliveredAt).HasColumnType("timestamptz");

        builder.HasIndex(n => n.ProviderMessageId).IsUnique().HasFilter("\"ProviderMessageId\" IS NOT NULL");
        builder.HasIndex(n => new { n.AppointmentId, n.ReminderSlot });
    }
}

public class TenantNotificationConfigConfiguration : IEntityTypeConfiguration<TenantNotificationConfig>
{
    public void Configure(EntityTypeBuilder<TenantNotificationConfig> builder)
    {
        builder.ToTable("tenant_notification_configs");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.TenantId).IsUnique();
    }
}
