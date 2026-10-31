using ErrorOr;

namespace DentFlow.Billing.Domain;

public static class BillingErrors
{
    public static readonly Error AppointmentNotFound =
        Error.NotFound("Invoice.AppointmentNotFound", "This patient has no such appointment.");

    public static readonly Error AppointmentAlreadyBilled =
        Error.Conflict("Invoice.AppointmentAlreadyBilled", "This appointment has already been billed. Void that invoice first to bill it again.");

    public static readonly Error InvoiceNotFound =
        Error.NotFound("Invoice.NotFound", "Invoice was not found.");

    public static readonly Error LineItemNotFound =
        Error.NotFound("Invoice.LineItemNotFound", "Invoice line item was not found.");

    public static readonly Error CannotModifyVoidedInvoice =
        Error.Conflict("Invoice.Voided", "Cannot modify a voided invoice.");

    public static readonly Error CannotModifyPaidInvoice =
        Error.Conflict("Invoice.Paid", "A fully paid invoice cannot be changed. Create a new invoice for additional charges.");

    public static readonly Error TotalBelowAmountPaid =
        Error.Validation("Invoice.TotalBelowPaid", "This change would make the invoice total lower than the amount already paid.");

    public static readonly Error CannotVoidPaidInvoice =
        Error.Conflict("Invoice.AlreadyPaid", "Cannot void a fully paid invoice.");

    public static readonly Error CannotVoidWithPayments =
        Error.Conflict("Invoice.HasPayments", "This invoice has payments recorded. Refund them before voiding the invoice.");

    public static readonly Error PaymentBeforeInvoice =
        Error.Validation("Invoice.PaymentBeforeInvoice", "A payment can't be dated before the invoice was issued.");

    public static readonly Error PaymentExceedsBalance =
        Error.Validation("Invoice.PaymentExceedsBalance", "Payment amount exceeds the remaining balance.");
}
