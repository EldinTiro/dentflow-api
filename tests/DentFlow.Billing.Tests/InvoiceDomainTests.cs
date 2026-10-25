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
    public void Void_SetsStatusToVoid_AndNothingIsOwed()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 45m));

        invoice.Void().IsError.Should().BeFalse();

        invoice.Status.Should().Be(InvoiceStatus.Void);
        invoice.BalanceDue.Should().Be(0m);
        invoice.IsOverdueAt(DateTime.UtcNow.AddYears(1)).Should().BeFalse();
    }

    // DT-03: a part-paid invoice was voided and its 10 KM stayed in revenue and cash-up
    [Fact]
    public void Void_WithAPartPayment_IsRefused_AndChangesNothing()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 45m));
        invoice.AddPayment(Payment(invoice.Id, 10m));

        invoice.Void().FirstError.Code.Should().Be(BillingErrors.CannotVoidWithPayments.Code);

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.BalanceDue.Should().Be(35m);
    }

    [Fact]
    public void Void_WhenFullyPaid_IsRefused()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 45m));
        invoice.AddPayment(Payment(invoice.Id, 45m));

        invoice.Void().FirstError.Code.Should().Be(BillingErrors.CannotVoidPaidInvoice.Code);
    }

    [Fact]
    public void Void_Twice_IsRefused()
    {
        var invoice = CreateInvoice();
        invoice.Void();

        invoice.Void().FirstError.Code.Should().Be(BillingErrors.CannotModifyVoidedInvoice.Code);
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

    // ── Line items vs. payments (QA-004) ─────────────────────────────────

    private static Invoice PaidInvoice(decimal total = 75m)
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, total));
        invoice.AddPayment(Payment(invoice.Id, total));
        return invoice;
    }

    private static Invoice PartiallyPaidInvoice(decimal total, decimal paid)
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, total));
        invoice.AddPayment(Payment(invoice.Id, paid));
        return invoice;
    }

    [Fact]
    public void AddLineItem_PaidInvoice_IsRejectedAndNothingChanges()
    {
        var invoice = PaidInvoice();

        var result = invoice.AddLineItem(Line(invoice.Id, 10m));

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.Paid");
        invoice.SubTotal.Should().Be(75m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void AddLineItem_VoidInvoice_IsRejected()
    {
        var invoice = CreateInvoice();
        invoice.Void();

        var result = invoice.AddLineItem(Line(invoice.Id, 10m));

        result.FirstError.Code.Should().Be("Invoice.Voided");
    }

    [Fact]
    public void RemoveLineItem_PaidInvoice_IsRejected()
    {
        var invoice = PaidInvoice();

        var result = invoice.RemoveLineItem(invoice.LineItems[0]);

        result.FirstError.Code.Should().Be("Invoice.Paid");
        invoice.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateLineItem_PaidInvoice_IsRejected()
    {
        var invoice = PaidInvoice();
        var item = invoice.LineItems[0];

        var result = invoice.UpdateLineItem(item, "Changed", null, null, 1, 85m);

        result.FirstError.Code.Should().Be("Invoice.Paid");
        item.UnitFee.Should().Be(75m);
    }

    [Fact]
    public void AddLineItem_PartiallyPaid_StaysPartiallyPaidWithLargerBalance()
    {
        var invoice = PartiallyPaidInvoice(100m, 40m);

        invoice.AddLineItem(Line(invoice.Id, 10m));

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.BalanceDue.Should().Be(70m);
    }

    [Fact]
    public void RemoveLineItem_PartiallyPaid_BalanceReachesZero_StatusBecomesPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line(invoice.Id, 60m));
        var extra = Line(invoice.Id, 40m);
        invoice.AddLineItem(extra);
        invoice.AddPayment(Payment(invoice.Id, 60m));

        var result = invoice.RemoveLineItem(extra);

        result.IsError.Should().BeFalse();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void UpdateLineItem_WouldDropTotalBelowAmountPaid_IsRejected()
    {
        var invoice = PartiallyPaidInvoice(100m, 80m);
        var item = invoice.LineItems[0];

        var result = invoice.UpdateLineItem(item, "Cheaper", null, null, 1, 50m);

        result.FirstError.Code.Should().Be("Invoice.TotalBelowPaid");
        item.UnitFee.Should().Be(100m);
    }

    [Fact]
    public void RemoveLineItem_WouldDropTotalBelowAmountPaid_IsRejected()
    {
        var invoice = CreateInvoice();
        var big = Line(invoice.Id, 100m);
        invoice.AddLineItem(big);
        invoice.AddLineItem(Line(invoice.Id, 20m));
        invoice.AddPayment(Payment(invoice.Id, 50m));

        var result = invoice.RemoveLineItem(big);

        result.FirstError.Code.Should().Be("Invoice.TotalBelowPaid");
        invoice.LineItems.Should().HaveCount(2);
    }

    // ── Overdue (QA-025) ─────────────────────────────────────────────────

    private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Invoice DueInvoice(DateTime? dueDate, decimal fee = 100m)
    {
        var invoice = Invoice.Create(Guid.NewGuid(), "INV-1", dueDate, null);
        invoice.AddLineItem(Line(invoice.Id, fee));
        return invoice;
    }

    [Fact]
    public void IsOverdue_UnpaidPastDueDate_IsTrue() =>
        DueInvoice(Now.AddDays(-1)).IsOverdueAt(Now).Should().BeTrue();

    [Fact]
    public void IsOverdue_BeforeDueDate_IsFalse() =>
        DueInvoice(Now.AddDays(1)).IsOverdueAt(Now).Should().BeFalse();

    [Fact]
    public void IsOverdue_NoDueDate_IsFalse() =>
        DueInvoice(null).IsOverdueAt(Now).Should().BeFalse();

    [Fact]
    public void IsOverdue_PaidOrVoid_IsFalse()
    {
        var paid = DueInvoice(Now.AddDays(-1));
        paid.AddPayment(Payment(paid.Id, 100m));
        var voided = DueInvoice(Now.AddDays(-1));
        voided.Void();

        paid.IsOverdueAt(Now).Should().BeFalse();
        voided.IsOverdueAt(Now).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_PartiallyPaidPastDueDate_IsTrue()
    {
        var invoice = DueInvoice(Now.AddDays(-1));
        invoice.AddPayment(Payment(invoice.Id, 40m));

        invoice.IsOverdueAt(Now).Should().BeTrue();
    }
}
