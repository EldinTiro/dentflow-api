using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DentFlow.Billing.Domain;

namespace DentFlow.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        // "Is this visit billed?" is asked for every appointment opened
        builder.HasIndex(i => new { i.TenantId, i.AppointmentId });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PatientId).IsRequired();
        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(2000);

        builder.HasIndex(i => new { i.TenantId, i.PatientId });
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber });
        builder.HasIndex(i => new { i.TenantId, i.Status });

        builder.HasMany(i => i.LineItems)
            .WithOne()
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("invoice_line_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceId).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.CdtCode).HasMaxLength(20);
        builder.Property(i => i.UnitFee).HasPrecision(10, 2);

        builder.HasIndex(i => new { i.TenantId, i.InvoiceId });
    }
}

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.ToTable("invoice_payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.InvoiceId).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(p => p.Method).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(500);

        builder.HasIndex(p => new { p.TenantId, p.InvoiceId });
    }
}
