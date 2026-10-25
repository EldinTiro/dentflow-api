using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Billing.Domain;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Billing.Application.Commands;

// ── Money rules (DT-11) ─────────────────────────────────────────────────────

/// <summary>
/// Amounts are whole cents, and a line stays far below NUMERIC(10,2) (99,999,999.99): a
/// 1e12 fee ended in a database overflow (500) and 0.001 became a 0.00 payment row.
/// </summary>
public static class MoneyRules
{
    public const decimal MaxUnitFee = 100_000m;
    public const int MaxQuantity = 100;

    public static bool IsWholeCents(decimal amount) => decimal.Round(amount, 2) == amount;
}

// ── Create Invoice ──────────────────────────────────────────────────────────

public record CreateInvoiceLineItemRequest(
    string Description,
    string? CdtCode,
    int? ToothNumber,
    int Quantity,
    decimal UnitFee,
    Guid? TreatmentPlanItemId);

public record CreateInvoiceCommand(
    Guid PatientId,
    DateTime? DueDate,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineItemRequest> LineItems)
    : IPatientScoped, IRequest<ErrorOr<InvoiceResponse>>;

public class CreateInvoiceCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<CreateInvoiceCommand, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var count = await repo.CountByTenantAsync(ct);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";

        var invoice = Invoice.Create(cmd.PatientId, invoiceNumber, cmd.DueDate, cmd.Notes);
        await repo.AddAsync(invoice, ct);

        foreach (var li in cmd.LineItems)
        {
            var lineItem = InvoiceLineItem.Create(
                invoice.Id, li.Description, li.CdtCode,
                li.ToothNumber, li.Quantity, li.UnitFee, li.TreatmentPlanItemId);
            await repo.AddLineItemAsync(lineItem, ct);
        }

        var created = await repo.GetByIdWithDetailsAsync(invoice.Id, ct);
        return InvoiceResponse.FromEntity(created!);
    }
}

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be today or in the future.");
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("An invoice needs at least one line.");
        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, MoneyRules.MaxQuantity);
            item.RuleFor(i => i.UnitFee).InclusiveBetween(0, MoneyRules.MaxUnitFee)
                .Must(MoneyRules.IsWholeCents).WithMessage("Amounts are in whole cents.");
        });
    }
}

// ── Update Invoice ──────────────────────────────────────────────────────────

public record UpdateInvoiceCommand(
    Guid InvoiceId,
    DateTime? DueDate,
    string? Notes)
    : IRequest<ErrorOr<InvoiceResponse>>;

public class UpdateInvoiceCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<UpdateInvoiceCommand, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(UpdateInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(cmd.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;
        if (invoice.Status == InvoiceStatus.Void) return BillingErrors.CannotModifyVoidedInvoice;

        invoice.Update(cmd.DueDate, cmd.Notes);
        await repo.UpdateAsync(invoice, ct);
        return InvoiceResponse.FromEntity(invoice);
    }
}

public class UpdateInvoiceCommandValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}

// ── Add Line Item ───────────────────────────────────────────────────────────

public record AddLineItemCommand(
    Guid InvoiceId,
    string Description,
    string? CdtCode,
    int? ToothNumber,
    int Quantity,
    decimal UnitFee,
    Guid? TreatmentPlanItemId)
    : IRequest<ErrorOr<InvoiceResponse>>;

public class AddLineItemCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<AddLineItemCommand, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(AddLineItemCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(cmd.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;

        var item = InvoiceLineItem.Create(
            invoice.Id, cmd.Description, cmd.CdtCode,
            cmd.ToothNumber, cmd.Quantity, cmd.UnitFee, cmd.TreatmentPlanItemId);
        var added = invoice.AddLineItem(item);
        if (added.IsError) return added.Errors;

        // Saves the new row and the invoice's recalculated status together.
        await repo.AddLineItemAsync(item, ct);
        return InvoiceResponse.FromEntity(invoice);
    }
}

public class AddLineItemCommandValidator : AbstractValidator<AddLineItemCommand>
{
    public AddLineItemCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).InclusiveBetween(1, MoneyRules.MaxQuantity);
        RuleFor(x => x.UnitFee).InclusiveBetween(0, MoneyRules.MaxUnitFee)
            .Must(MoneyRules.IsWholeCents).WithMessage("Amounts are in whole cents.");
    }
}

// ── Update Line Item ────────────────────────────────────────────────────────

public record UpdateLineItemCommand(
    Guid InvoiceId,
    Guid LineItemId,
    string Description,
    string? CdtCode,
    int? ToothNumber,
    int Quantity,
    decimal UnitFee)
    : IRequest<ErrorOr<InvoiceResponse>>;

public class UpdateLineItemCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<UpdateLineItemCommand, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(UpdateLineItemCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(cmd.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;

        var item = invoice.LineItems.FirstOrDefault(li => li.Id == cmd.LineItemId);
        if (item is null) return BillingErrors.LineItemNotFound;

        var updated = invoice.UpdateLineItem(item, cmd.Description, cmd.CdtCode, cmd.ToothNumber, cmd.Quantity, cmd.UnitFee);
        if (updated.IsError) return updated.Errors;

        await repo.UpdateAsync(invoice, ct);
        return InvoiceResponse.FromEntity(invoice);
    }
}

public class UpdateLineItemCommandValidator : AbstractValidator<UpdateLineItemCommand>
{
    public UpdateLineItemCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.LineItemId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).InclusiveBetween(1, MoneyRules.MaxQuantity);
        RuleFor(x => x.UnitFee).InclusiveBetween(0, MoneyRules.MaxUnitFee)
            .Must(MoneyRules.IsWholeCents).WithMessage("Amounts are in whole cents.");
    }
}

// ── Delete Line Item ────────────────────────────────────────────────────────

public record DeleteLineItemCommand(Guid InvoiceId, Guid LineItemId) : IRequest<ErrorOr<Success>>;

public class DeleteLineItemCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<DeleteLineItemCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(DeleteLineItemCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(cmd.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;

        var item = invoice.LineItems.FirstOrDefault(li => li.Id == cmd.LineItemId);
        if (item is null) return BillingErrors.LineItemNotFound;

        var removed = invoice.RemoveLineItem(item);
        if (removed.IsError) return removed.Errors;

        // Soft-deletes the row and saves the invoice's recalculated status together.
        await repo.RemoveLineItemAsync(item, ct);
        return Result.Success;
    }
}

// ── Void Invoice ────────────────────────────────────────────────────────────

public record VoidInvoiceCommand(Guid InvoiceId) : IRequest<ErrorOr<InvoiceResponse>>;

public class VoidInvoiceCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<VoidInvoiceCommand, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(VoidInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(cmd.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;
        var voided = invoice.Void();
        if (voided.IsError) return voided.Errors;

        await repo.UpdateAsync(invoice, ct);
        return InvoiceResponse.FromEntity(invoice);
    }
}

// ── Record Payment ──────────────────────────────────────────────────────────

public record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTime PaidAt,
    string? Reference,
    string? Notes)
    : IRequest<ErrorOr<InvoiceResponse>>;

public class RecordPaymentCommandHandler(IInvoiceRepository repo)
    : IRequestHandler<RecordPaymentCommand, ErrorOr<InvoiceResponse>>
{
    private static readonly string[] AllowedMethods =
        Enum.GetNames<PaymentMethod>();

    public async Task<ErrorOr<InvoiceResponse>> Handle(RecordPaymentCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(cmd.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;
        if (invoice.Status == InvoiceStatus.Void) return BillingErrors.CannotModifyVoidedInvoice;
        if (cmd.Amount > invoice.BalanceDue) return BillingErrors.PaymentExceedsBalance;
        // A day of slack: the invoice is stamped in UTC, the payment date is picked in local time.
        if (cmd.PaidAt < invoice.IssuedAt.Date.AddDays(-1)) return BillingErrors.PaymentBeforeInvoice;

        var method = Enum.Parse<PaymentMethod>(cmd.Method, ignoreCase: true);
        var payment = InvoicePayment.Create(invoice.Id, cmd.Amount, method, cmd.PaidAt, cmd.Reference, cmd.Notes);

        // Aggregate first, then persist. The other way round, EF's relationship fix-up
        // had already put the tracked payment into Payments, AddPayment added it again,
        // and a partial payment was stored as Paid.
        invoice.AddPayment(payment);
        await repo.AddPaymentAsync(payment, ct);
        return InvoiceResponse.FromEntity(invoice);
    }
}

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    private static readonly string[] AllowedMethods = Enum.GetNames<PaymentMethod>();

    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0.01m)
            .Must(MoneyRules.IsWholeCents).WithMessage("Amounts are in whole cents.");
        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(v => AllowedMethods.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Method must be one of: {string.Join(", ", AllowedMethods)}.");
        // Evaluated per validation, with a little slack for client clock skew.
        RuleFor(x => x.PaidAt)
            .Must(paidAt => paidAt <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Payment date cannot be in the future.");
    }
}
