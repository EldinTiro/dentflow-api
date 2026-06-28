using FluentAssertions;
using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Tests;

/// <summary>
/// Pure domain tests — no mocks, no I/O. These test the Invoice aggregate's
/// balance calculations, status machine, and payment logic in isolation.
/// </summary>
public class InvoiceDomainTests
{
    private static Invoice CreateInvoice() =>
        Invoice.Create(Guid.NewGuid(), "INV-20240101-0001", null, null);

    private static InvoiceLineItem Line(Guid invoiceId, decimal unitFee, int qty = 1) =>
        InvoiceLineItem.Create(invoiceId, "Checkup", null, null, qty, unitFee);

    private static InvoicePayment Payment(Guid invoiceId, decimal amount) =>
        InvoicePayment.Create(invoiceId, amount, PaymentMethod.Cash, DateTime.UtcNow, null, null);

    // ── Balance calculations ──────────────────────────────────────────────

    [Fact]
    public void SubTotal_NoLineItems_IsZero()
    {
        var invoice = CreateInvoice();
        invoice.SubTotal.Should().Be(0m);
    }

    [Fact]
    public void SubTotal_MultipleItems_SumsCorrectly()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 100m));
        invoice.AddLineItem(Line(invoice.Id, 50m, qty: 2));

        invoice.SubTotal.Should().Be(200m); // 100 + (50 * 2)
    }

    [Fact]
    public void BalanceDue_WithNoPayments_EqualsSubTotal()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 150m));

        invoice.BalanceDue.Should().Be(150m);
    }

    [Fact]
    public void BalanceDue_AfterPartialPayment_ReducesCorrectly()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 200m));
        invoice.AddPayment(Payment(invoice.Id, 80m));

        invoice.BalanceDue.Should().Be(120m);
        invoice.PaidAmount.Should().Be(80m);
    }

    [Fact]
    public void BalanceDue_AfterFullPayment_IsZero()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 100m));
        invoice.AddPayment(Payment(invoice.Id, 100m));

        invoice.BalanceDue.Should().Be(0m);
    }

    // ── Status machine ────────────────────────────────────────────────────

    [Fact]
    public void NewInvoice_StatusIsDraft()
    {
        var invoice = CreateInvoice();
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void AfterPartialPayment_StatusIsPartiallyPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 200m));
        invoice.AddPayment(Payment(invoice.Id, 50m));

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void AfterFullPayment_StatusIsPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 100m));
        invoice.AddPayment(Payment(invoice.Id, 100m));

        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void AfterTwoPartialPayments_FullCoverage_StatusIsPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 300m));
        invoice.AddPayment(Payment(invoice.Id, 200m));
        invoice.AddPayment(Payment(invoice.Id, 100m));

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public void Void_SetsStatusToVoid()
    {
        var invoice = CreateInvoice();
        invoice.Void();
        invoice.Status.Should().Be(InvoiceStatus.Void);
    }

    [Fact]
    public void MarkAsSent_FromDraft_SetsStatusToSent()
    {
        var invoice = CreateInvoice();
        invoice.MarkAsSent();
        invoice.Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public void MarkAsSent_AlreadySent_DoesNotChangeStatus()
    {
        var invoice = CreateInvoice();
        invoice.MarkAsSent();
        invoice.MarkAsSent(); // idempotent call
        invoice.Status.Should().Be(InvoiceStatus.Sent);
    }

    // ── Line item management ──────────────────────────────────────────────

    [Fact]
    public void RemoveLineItem_ReducesSubTotal()
    {
        var invoice = CreateInvoice();
        var item = Line(invoice.Id, 100m);
        invoice.AddLineItem(item);
        invoice.AddLineItem(Line(invoice.Id, 50m));

        invoice.RemoveLineItem(item);

        invoice.SubTotal.Should().Be(50m);
        invoice.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void MultiplePayments_PaidAmountSumsAll()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 1000m));
        invoice.AddPayment(Payment(invoice.Id, 300m));
        invoice.AddPayment(Payment(invoice.Id, 300m));
        invoice.AddPayment(Payment(invoice.Id, 400m));

        invoice.PaidAmount.Should().Be(1000m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }
}
