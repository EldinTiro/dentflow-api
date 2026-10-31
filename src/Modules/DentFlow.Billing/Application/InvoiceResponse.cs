using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Application;

public record InvoicePaymentResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? Reference,
    string? Notes)
{
    public static InvoicePaymentResponse FromEntity(InvoicePayment p) =>
        new(p.Id, p.InvoiceId, p.Amount, p.Method.ToString(), p.PaidAt, p.Reference, p.Notes);
}

public record InvoiceLineItemResponse(
    Guid Id,
    Guid InvoiceId,
    string Description,
    string? CdtCode,
    int? ToothNumber,
    int Quantity,
    decimal UnitFee,
    decimal LineTotal,
    Guid? TreatmentPlanItemId)
{
    public static InvoiceLineItemResponse FromEntity(InvoiceLineItem i) =>
        new(i.Id, i.InvoiceId, i.Description, i.CdtCode, i.ToothNumber,
            i.Quantity, i.UnitFee, i.LineTotal, i.TreatmentPlanItemId);
}

public record InvoiceResponse(
    Guid Id,
    Guid PatientId,
    string InvoiceNumber,
    string Status,
    DateTime IssuedAt,
    DateTime? DueDate,
    string? Notes,
    decimal SubTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    IReadOnlyList<InvoiceLineItemResponse> LineItems,
    IReadOnlyList<InvoicePaymentResponse> Payments,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? AppointmentId)
{
    public static InvoiceResponse FromEntity(Invoice inv) =>
        new(inv.Id, inv.PatientId, inv.InvoiceNumber, inv.Status.ToString(),
            inv.IssuedAt, inv.DueDate, inv.Notes, inv.SubTotal, inv.PaidAmount, inv.BalanceDue,
            inv.LineItems.Select(InvoiceLineItemResponse.FromEntity).ToList().AsReadOnly(),
            inv.Payments.Select(InvoicePaymentResponse.FromEntity).ToList().AsReadOnly(),
            inv.CreatedAt, inv.UpdatedAt, inv.AppointmentId);
}

public record InvoiceSummaryResponse(
    Guid Id,
    Guid PatientId,
    string InvoiceNumber,
    string Status,
    DateTime IssuedAt,
    DateTime? DueDate,
    decimal SubTotal,
    decimal PaidAmount,
    decimal BalanceDue,
    int LineItemCount,
    DateTime CreatedAt,
    string? PatientName,
    bool IsOverdue,
    Guid? AppointmentId)
{
    public static InvoiceSummaryResponse FromEntity(Invoice inv, string? patientName, DateTime utcNow) =>
        new(inv.Id, inv.PatientId, inv.InvoiceNumber, inv.Status.ToString(),
            inv.IssuedAt, inv.DueDate, inv.SubTotal, inv.PaidAmount, inv.BalanceDue,
            inv.LineItems.Count, inv.CreatedAt, patientName, inv.IsOverdueAt(utcNow), inv.AppointmentId);
}
