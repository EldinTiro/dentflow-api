using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Appointments.Domain;
using DentFlow.Billing.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure;

public static class InvoiceSeeder
{
    private static readonly (string Description, string CdtCode, decimal Fee)[] LineCatalog =
    [
        ("Periodic Oral Evaluation",       "D0120", 85m),
        ("Comprehensive Oral Evaluation",  "D0150", 120m),
        ("Intraoral X-rays (full series)", "D0210", 175m),
        ("Prophylaxis – Adult",            "D1110", 95m),
        ("Prophylaxis – Child",            "D1120", 75m),
        ("Amalgam Filling – 1 surface",    "D2140", 145m),
        ("Composite Filling – 1 surface",  "D2330", 185m),
        ("Composite Filling – 2 surfaces", "D2331", 235m),
        ("Porcelain Crown",                "D2740", 1100m),
        ("Root Canal – anterior",          "D3310", 750m),
        ("Root Canal – premolar",          "D3320", 850m),
        ("Extraction – simple",            "D7140", 175m),
        ("In-office Whitening",            "D9975", 450m),
        ("Full-mouth Debridement",         "D4355", 225m),
        ("Scaling & Root Planing",         "D4341", 295m),
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

            var existing = await db.Set<Invoice>()
                .IgnoreQueryFilters()
                .CountAsync(i => i.TenantId == tenantId);

            if (existing >= 20)
            {
                logger.LogDebug("Invoice seed already present for tenant {TenantName} ({Count} invoices)", tenant.Name, existing);
                continue;
            }

            logger.LogInformation("Seeding invoices for tenant {TenantName}", tenant.Name);

            // Get completed past appointments to invoice
            var today = DateTime.UtcNow;
            var completedAppts = await db.Set<Appointment>()
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == tenantId &&
                            a.Status == AppointmentStatus.Completed &&
                            a.StartAt < today)
                .OrderByDescending(a => a.StartAt)
                .Take(300)
                .ToListAsync();

            if (completedAppts.Count == 0)
            {
                logger.LogWarning("No completed appointments found for invoice seeding on tenant {TenantName}", tenant.Name);
                continue;
            }

            var rng = new Random(99);
            int invoiceSeq = existing + 1;
            int totalSeeded = 0;

            // Invoice roughly 70% of completed appointments
            foreach (var appt in completedAppts)
            {
                if (rng.Next(10) >= 7) continue; // skip 30%

                // Same scheme as CreateInvoiceCommandHandler (QA-025).
                var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{invoiceSeq:D4}";
                invoiceSeq++;

                // Issued now (Invoice.Create), so due 30 days from now — not before the issue date.
                var dueDate = DateTime.UtcNow.AddDays(30);
                var invoice = Invoice.Create(appt.PatientId, invoiceNumber, dueDate, null, appt.Id);
                invoice.SetTenant(tenantId);

                // Add 1-3 line items
                int lineCount = rng.Next(1, 4);
                for (int j = 0; j < lineCount; j++)
                {
                    var (desc, cdt, fee) = LineCatalog[rng.Next(LineCatalog.Length)];
                    var lineItem = InvoiceLineItem.Create(invoice.Id, desc, cdt, null, 1, fee);
                    lineItem.SetTenant(tenantId);
                    invoice.AddLineItem(lineItem);
                }

                invoice.MarkAsSent();

                // Determine payment status
                int payRoll = rng.Next(100);
                var subTotal = invoice.LineItems.Sum(li => li.LineTotal);

                if (payRoll < 60)
                {
                    // Fully paid
                    var payment = InvoicePayment.Create(
                        invoice.Id,
                        subTotal,
                        (PaymentMethod)rng.Next(3), // Cash, Card, Insurance
                        appt.StartAt.AddDays(rng.Next(1, 10)),
                        null,
                        null);
                    payment.SetTenant(tenantId);
                    invoice.AddPayment(payment);
                }
                else if (payRoll < 80)
                {
                    // Partial payment
                    var partial = Math.Round(subTotal * (decimal)(rng.NextDouble() * 0.5 + 0.2), 2);
                    var payment = InvoicePayment.Create(
                        invoice.Id,
                        partial,
                        (PaymentMethod)rng.Next(3),
                        appt.StartAt.AddDays(rng.Next(1, 15)),
                        null,
                        "Partial payment");
                    payment.SetTenant(tenantId);
                    invoice.AddPayment(payment);
                }
                // else: unpaid (stays Sent)

                db.Set<Invoice>().Add(invoice);
                totalSeeded++;
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} invoices for tenant {TenantName}", totalSeeded, tenant.Name);
        }
    }
}
