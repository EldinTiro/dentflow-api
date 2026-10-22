using ErrorOr;

namespace DentFlow.Billing.Domain;

public static class BillingErrors
{
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

    public static readonly Error PaymentExceedsBalance =
        Error.Validation("Invoice.PaymentExceedsBalance", "Payment amount exceeds the remaining balance.");
}
