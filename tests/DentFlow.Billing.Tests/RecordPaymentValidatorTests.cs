using FluentAssertions;
using DentFlow.Billing.Application.Commands;

namespace DentFlow.Billing.Tests;

public class RecordPaymentValidatorTests
{
    private readonly RecordPaymentCommandValidator _sut = new();

    private static RecordPaymentCommand ValidCommand(DateTime? paidAt = null) =>
        new(Guid.NewGuid(), 50m, "Cash", paidAt ?? DateTime.UtcNow, null, null);

    [Fact]
    public void PaidAt_Now_IsValid() =>
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();

    // QA-009: the raw rule message leaked to the UI ("'paid At' must be less than or equal to '…'")
    [Fact]
    public void PaidAt_InTheFuture_HasAReadableMessage()
    {
        var result = _sut.Validate(ValidCommand(DateTime.UtcNow.AddHours(3)));

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Payment date cannot be in the future.");
    }
}
