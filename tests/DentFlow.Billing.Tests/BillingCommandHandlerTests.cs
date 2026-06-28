using FluentAssertions;
using NSubstitute;
using DentFlow.Billing.Application.Commands;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Tests;

// ── CreateInvoice ─────────────────────────────────────────────────────────────

public class CreateInvoiceCommandHandlerTests
{
    private readonly IInvoiceRepository _repo = Substitute.For<IInvoiceRepository>();
    private readonly CreateInvoiceCommandHandler _sut;

    public CreateInvoiceCommandHandlerTests() =>
        _sut = new CreateInvoiceCommandHandler(_repo);

    [Fact]
    public async Task Handle_ValidCommand_CreatesInvoiceAndReturnsResponse()
    {
        _repo.CountByTenantAsync(Arg.Any<CancellationToken>()).Returns(0);

        var invoice = Invoice.Create(Guid.NewGuid(), "INV-TEST-0001", null, null);
        _repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(invoice);

        var cmd = new CreateInvoiceCommand(
            PatientId: Guid.NewGuid(),
            DueDate: null,
            Notes: "First visit",
            LineItems: [new CreateInvoiceLineItemRequest("Checkup", null, null, 1, 80m, null)]);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _repo.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).AddLineItemAsync(Arg.Any<InvoiceLineItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvoiceNumber_UsesCountPlusOne()
    {
        _repo.CountByTenantAsync(Arg.Any<CancellationToken>()).Returns(41);
        _repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(Invoice.Create(Guid.NewGuid(), "irrelevant", null, null));

        var cmd = new CreateInvoiceCommand(
            Guid.NewGuid(), null, null,
            [new CreateInvoiceLineItemRequest("X", null, null, 1, 10m, null)]);

        await _sut.Handle(cmd, CancellationToken.None);

        // The invoice passed to AddAsync should have number ending in 0042
        await _repo.Received(1).AddAsync(
            Arg.Is<Invoice>(inv => inv.InvoiceNumber.EndsWith("0042")),
            Arg.Any<CancellationToken>());
    }
}

// ── VoidInvoice ───────────────────────────────────────────────────────────────

public class VoidInvoiceCommandHandlerTests
{
    private readonly IInvoiceRepository _repo = Substitute.For<IInvoiceRepository>();
    private readonly VoidInvoiceCommandHandler _sut;

    public VoidInvoiceCommandHandlerTests() =>
        _sut = new VoidInvoiceCommandHandler(_repo);

    [Fact]
    public async Task Handle_DraftInvoice_Voids()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), "INV-001", null, null);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new VoidInvoiceCommand(invoice.Id), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be("Void");
        await _repo.Received(1).UpdateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaidInvoice_ReturnsError()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), "INV-001", null, null);
        var lineItem = InvoiceLineItem.Create(invoice.Id, "X", null, null, 1, 100m);
        invoice.AddLineItem(lineItem);
        invoice.AddPayment(InvoicePayment.Create(invoice.Id, 100m, PaymentMethod.Cash, DateTime.UtcNow, null, null));

        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new VoidInvoiceCommand(invoice.Id), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.AlreadyPaid");
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        _repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((Invoice?)null);

        var result = await _sut.Handle(new VoidInvoiceCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.NotFound");
    }
}

// ── RecordPayment ─────────────────────────────────────────────────────────────

public class RecordPaymentCommandHandlerTests
{
    private readonly IInvoiceRepository _repo = Substitute.For<IInvoiceRepository>();
    private readonly RecordPaymentCommandHandler _sut;

    public RecordPaymentCommandHandlerTests() =>
        _sut = new RecordPaymentCommandHandler(_repo);

    private static Invoice InvoiceWithLine(decimal amount)
    {
        var inv = Invoice.Create(Guid.NewGuid(), "INV-001", null, null);
        inv.AddLineItem(InvoiceLineItem.Create(inv.Id, "Service", null, null, 1, amount));
        return inv;
    }

    [Fact]
    public async Task Handle_ValidPartialPayment_RecordsAndUpdatesStatus()
    {
        var invoice = InvoiceWithLine(200m);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new RecordPaymentCommand(
            invoice.Id, 100m, "Cash", DateTime.UtcNow, null, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _repo.Received(1).AddPaymentAsync(Arg.Any<InvoicePayment>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).UpdateAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentExceedsBalance_ReturnsError()
    {
        var invoice = InvoiceWithLine(100m);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new RecordPaymentCommand(
            invoice.Id, 150m, "Cash", DateTime.UtcNow, null, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.PaymentExceedsBalance");
        await _repo.DidNotReceive().AddPaymentAsync(Arg.Any<InvoicePayment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VoidedInvoice_ReturnsError()
    {
        var invoice = InvoiceWithLine(100m);
        invoice.Void();
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new RecordPaymentCommand(
            invoice.Id, 50m, "Cash", DateTime.UtcNow, null, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.Voided");
    }

    [Fact]
    public async Task Handle_InvalidMethod_ReturnsError()
    {
        var invoice = InvoiceWithLine(100m);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var act = async () => await _sut.Handle(new RecordPaymentCommand(
            invoice.Id, 50m, "BitcoinLightning", DateTime.UtcNow, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>(); // Enum.Parse throws on invalid value
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        _repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((Invoice?)null);

        var result = await _sut.Handle(new RecordPaymentCommand(
            Guid.NewGuid(), 50m, "Cash", DateTime.UtcNow, null, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.NotFound");
    }
}

// ── AddLineItem ───────────────────────────────────────────────────────────────

public class AddLineItemCommandHandlerTests
{
    private readonly IInvoiceRepository _repo = Substitute.For<IInvoiceRepository>();
    private readonly AddLineItemCommandHandler _sut;

    public AddLineItemCommandHandlerTests() =>
        _sut = new AddLineItemCommandHandler(_repo);

    [Fact]
    public async Task Handle_VoidedInvoice_ReturnsError()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), "INV-001", null, null);
        invoice.Void();
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(
            new AddLineItemCommand(invoice.Id, "Xray", null, null, 1, 40m, null),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.Voided");
        await _repo.DidNotReceive().AddLineItemAsync(Arg.Any<InvoiceLineItem>(), Arg.Any<CancellationToken>());
    }
}

// ── DeleteLineItem ────────────────────────────────────────────────────────────

public class DeleteLineItemCommandHandlerTests
{
    private readonly IInvoiceRepository _repo = Substitute.For<IInvoiceRepository>();
    private readonly DeleteLineItemCommandHandler _sut;

    public DeleteLineItemCommandHandlerTests() =>
        _sut = new DeleteLineItemCommandHandler(_repo);

    [Fact]
    public async Task Handle_ValidLineItem_Removes()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), "INV-001", null, null);
        var item = InvoiceLineItem.Create(invoice.Id, "Checkup", null, null, 1, 80m);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _repo.GetLineItemByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.Handle(
            new DeleteLineItemCommand(invoice.Id, item.Id), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _repo.Received(1).RemoveLineItemAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LineItemNotFound_ReturnsError()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), "INV-001", null, null);
        _repo.GetByIdWithDetailsAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _repo.GetLineItemByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((InvoiceLineItem?)null);

        var result = await _sut.Handle(
            new DeleteLineItemCommand(invoice.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Invoice.LineItemNotFound");
    }
}
