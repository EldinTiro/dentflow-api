using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Invoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(
        Guid? patientId,
        InvoiceStatus? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default,
        Guid? appointmentId = null);
    /// <summary>Whether a non-void invoice already bills this appointment.</summary>
    Task<bool> HasActiveInvoiceForAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
    Task<int> CountByTenantAsync(CancellationToken ct = default);
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
    Task<InvoiceLineItem?> GetLineItemByIdAsync(Guid lineItemId, CancellationToken ct = default);
    Task AddLineItemAsync(InvoiceLineItem lineItem, CancellationToken ct = default);
    Task RemoveLineItemAsync(InvoiceLineItem lineItem, CancellationToken ct = default);
    Task AddPaymentAsync(InvoicePayment payment, CancellationToken ct = default);
}
