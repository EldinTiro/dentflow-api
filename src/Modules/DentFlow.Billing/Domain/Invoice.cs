using DentFlow.Domain.Common;
using ErrorOr;

namespace DentFlow.Billing.Domain;

public class Invoice : TenantAuditableEntity
{
    public Guid PatientId { get; private set; }
    public string InvoiceNumber { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<InvoiceLineItem> _lineItems = [];
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    private readonly List<InvoicePayment> _payments = [];
    public IReadOnlyList<InvoicePayment> Payments => _payments.AsReadOnly();

    public decimal SubTotal => _lineItems.Sum(i => i.LineTotal);
    public decimal PaidAmount => _payments.Sum(p => p.Amount);
    // A void invoice is not owed (DT-03); it can only be voided before any payment.
    public decimal BalanceDue => Status == InvoiceStatus.Void ? 0m : SubTotal - PaidAmount;

    /// <summary>Past its due date with money still owed (QA-025).</summary>
    public bool IsOverdueAt(DateTime utcNow) =>
        Status is not (InvoiceStatus.Paid or InvoiceStatus.Void)
        && DueDate is { } due && due < utcNow
        && BalanceDue > 0;

    private Invoice() { }

    public static Invoice Create(Guid patientId, string invoiceNumber, DateTime? dueDate, string? notes) =>
        new Invoice
        {
            PatientId = patientId,
            InvoiceNumber = invoiceNumber,
            Status = InvoiceStatus.Draft,
            IssuedAt = DateTime.UtcNow,
            DueDate = dueDate,
            Notes = notes
        };

    public void Update(DateTime? dueDate, string? notes)
    {
        DueDate = dueDate;
        Notes = notes;
        SetUpdated();
    }

    // Line items are frozen once fully paid, and can never total less than what has
    // been paid. Otherwise a paid invoice could show "Paid" with a balance that the UI
    // would never let anyone collect (QA-004).

    public ErrorOr<Success> AddLineItem(InvoiceLineItem item)
    {
        var editable = EnsureLineItemsEditable();
        if (editable.IsError) return editable.Errors;

        _lineItems.Add(item);
        RecalculateStatus();
        return Result.Success;
    }

    public ErrorOr<Success> UpdateLineItem(
        InvoiceLineItem item, string description, string? cdtCode, int? toothNumber, int quantity, decimal unitFee)
    {
        var editable = EnsureLineItemsEditable();
        if (editable.IsError) return editable.Errors;

        if (SubTotal - item.LineTotal + quantity * unitFee < PaidAmount)
            return BillingErrors.TotalBelowAmountPaid;

        item.Update(description, cdtCode, toothNumber, quantity, unitFee);
        RecalculateStatus();
        return Result.Success;
    }

    public ErrorOr<Success> RemoveLineItem(InvoiceLineItem item)
    {
        var editable = EnsureLineItemsEditable();
        if (editable.IsError) return editable.Errors;

        if (SubTotal - item.LineTotal < PaidAmount)
            return BillingErrors.TotalBelowAmountPaid;

        _lineItems.Remove(item);
        RecalculateStatus();
        return Result.Success;
    }

    private ErrorOr<Success> EnsureLineItemsEditable() => Status switch
    {
        InvoiceStatus.Void => BillingErrors.CannotModifyVoidedInvoice,
        InvoiceStatus.Paid => BillingErrors.CannotModifyPaidInvoice,
        _ => Result.Success,
    };

    public void MarkAsSent()
    {
        if (Status == InvoiceStatus.Draft)
            Status = InvoiceStatus.Sent;
        SetUpdated();
    }

    public void AddPayment(InvoicePayment payment)
    {
        _payments.Add(payment);
        RecalculateStatus();
        SetUpdated();
    }

    /// <summary>
    /// DT-03: only an invoice with no payment can be voided. With money received the
    /// payment has to be refunded first, or it would stay in revenue and cash-up with no
    /// invoice behind it.
    /// </summary>
    public ErrorOr<Success> Void()
    {
        if (Status == InvoiceStatus.Void) return BillingErrors.CannotModifyVoidedInvoice;
        if (Status == InvoiceStatus.Paid) return BillingErrors.CannotVoidPaidInvoice;
        if (PaidAmount > 0) return BillingErrors.CannotVoidWithPayments;

        Status = InvoiceStatus.Void;
        SetUpdated();
        return Result.Success;
    }

    private void RecalculateStatus()
    {
        if (PaidAmount <= 0) return; // Draft / Sent are not payment-driven

        Status = BalanceDue <= 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }
}
