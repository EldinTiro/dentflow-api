using FluentAssertions;
using DentFlow.Billing.Application.Commands;

namespace DentFlow.Billing.Tests;

/// <summary>
/// QA pass 2026-10-31 (N-09): treatment-plan items and the tooth chart require an FDI tooth
/// number (11–48), but an invoice line accepted 5 and 99. The same tooth must mean the same
/// thing on the plan, the chart and the bill.
/// </summary>
public class InvoiceToothNumberTests
{
    private static CreateInvoiceCommand Invoice(int? tooth) =>
        new(Guid.NewGuid(), null, null, [new CreateInvoiceLineItemRequest("Filling", null, tooth, 1, 60m, null)]);

    [Theory]
    [InlineData(5)]
    [InlineData(19)]
    [InlineData(99)]
    public void AnInvoiceLineWithANonFdiTooth_IsRejected(int tooth)
    {
        new CreateInvoiceCommandValidator().Validate(Invoice(tooth)).IsValid.Should().BeFalse();
        new AddLineItemCommandValidator().Validate(new AddLineItemCommand(Guid.NewGuid(), "x", null, tooth, 1, 10m, null))
            .IsValid.Should().BeFalse();
        new UpdateLineItemCommandValidator().Validate(new UpdateLineItemCommand(Guid.NewGuid(), Guid.NewGuid(), "x", null, tooth, 1, 10m))
            .IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(11)]
    [InlineData(48)]
    [InlineData(null)]
    public void AnFdiToothOrNoTooth_IsValid(int? tooth) =>
        new CreateInvoiceCommandValidator().Validate(Invoice(tooth)).IsValid.Should().BeTrue();
}
