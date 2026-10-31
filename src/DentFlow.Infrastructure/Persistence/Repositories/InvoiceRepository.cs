using Microsoft.EntityFrameworkCore;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Billing.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(ApplicationDbContext dbContext) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct) =>
        dbContext.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct) =>
        dbContext.Set<Invoice>()
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(
        Guid? patientId,
        InvoiceStatus? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct,
        Guid? appointmentId = null)
    {
        var query = dbContext.Set<Invoice>()
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(i => i.PatientId == patientId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (appointmentId.HasValue)
            query = query.Where(i => i.AppointmentId == appointmentId.Value);

        if (from.HasValue) query = query.Where(i => i.IssuedAt >= from.Value);
        if (to.HasValue) query = query.Where(i => i.IssuedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> HasActiveInvoiceForAppointmentAsync(Guid appointmentId, CancellationToken ct = default) =>
        dbContext.Set<Invoice>().AnyAsync(i => i.AppointmentId == appointmentId && i.Status != InvoiceStatus.Void, ct);

    public async Task<int> CountByTenantAsync(CancellationToken ct) =>
        await dbContext.Set<Invoice>().CountAsync(ct);

    public async Task AddAsync(Invoice invoice, CancellationToken ct)
    {
        await dbContext.Set<Invoice>().AddAsync(invoice, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct)
    {
        dbContext.Set<Invoice>().Update(invoice);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<InvoiceLineItem?> GetLineItemByIdAsync(Guid lineItemId, CancellationToken ct) =>
        dbContext.Set<InvoiceLineItem>().FirstOrDefaultAsync(i => i.Id == lineItemId, ct);

    public async Task AddLineItemAsync(InvoiceLineItem lineItem, CancellationToken ct)
    {
        await dbContext.Set<InvoiceLineItem>().AddAsync(lineItem, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task RemoveLineItemAsync(InvoiceLineItem lineItem, CancellationToken ct)
    {
        lineItem.SoftDelete();
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task AddPaymentAsync(InvoicePayment payment, CancellationToken ct)
    {
        await dbContext.Set<InvoicePayment>().AddAsync(payment, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}
