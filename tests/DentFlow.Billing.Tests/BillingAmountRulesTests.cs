using FluentAssertions;
using NSubstitute;
using DentFlow.Billing.Application.Commands;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Tests;

/// <summary>
/// DT-11: 0.001 became a 0.00 KM payment row, 1e12 ended in a NUMERIC overflow (500),
/// an invoice with no lines was accepted, and a payment could be dated 1990.
/// </summary>
public class BillingAmountRulesTests
{
    private static CreateInvoiceLineItemRequest Line(decimal unitFee = 45m, int quantity = 1) =>
        new("Check-up", null, null, quantity, unitFee, null);

    private static CreateInvoiceCommand Invoice(params CreateInvoiceLineItemRequest[] lines) =>
        new(Guid.NewGuid(), null, null, lines);

    [Fact]
    public void AnInvoiceWithLines_IsValid() =>
        new CreateInvoiceCommandValidator().Validate(Invoice(Line())).IsValid.Should().BeTrue();

    [Fact]
    public void AnInvoiceWithNoLines_IsRejected() =>
        new CreateInvoiceCommandValidator().Validate(Invoice()).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0.005)]          // fractions of a cent
    [InlineData(1e12)]           // would overflow NUMERIC(10,2)
    [InlineData(100000.01)]      // above the per-line cap
    public void AFeeThatIsNotWholeCentsOrTooLarge_IsRejected(double fee)
    {
        new CreateInvoiceCommandValidator().Validate(Invoice(Line((decimal)fee))).IsValid.Should().BeFalse();
        new AddLineItemCommandValidator().Validate(new AddLineItemCommand(Guid.NewGuid(), "x", null, null, 1, (decimal)fee, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void ATooLargeQuantity_IsRejected() =>
        new CreateInvoiceCommandValidator().Validate(Invoice(Line(quantity: 101))).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0.001)]
    [InlineData(10.005)]
    public void APaymentThatIsNotWholeCents_IsRejected(double amount) =>
        new RecordPaymentCommandValidator()
            .Validate(new RecordPaymentCommand(Guid.NewGuid(), (decimal)amount, "Cash", DateTime.UtcNow, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public async Task APaymentDatedBeforeTheInvoice_IsRefused()
    {
        var repo = Substitute.For<IInvoiceRepository>();
        var invoice = DentFlow.Billing.Domain.Invoice.Create(Guid.NewGuid(), "INV-1", null, null);
        invoice.AddLineItem(InvoiceLineItem.Create(invoice.Id, "x", null, null, 1, 50m));
        repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await new RecordPaymentCommandHandler(repo).Handle(
            new RecordPaymentCommand(invoice.Id, 10m, "Cash", new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, null), default);

        result.FirstError.Code.Should().Be(BillingErrors.PaymentBeforeInvoice.Code);
        invoice.Payments.Should().BeEmpty();
    }
}
