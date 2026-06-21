using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Application.Commands;

public record CreateInvoiceFromTreatmentPlanCommand(
    Guid TreatmentPlanId,
    DateTime? DueDate,
    string? Notes)
    : IRequest<ErrorOr<InvoiceResponse>>;

public class CreateInvoiceFromTreatmentPlanCommandHandler(
    IInvoiceRepository repo,
    ITreatmentPlanReader planReader)
    : IRequestHandler<CreateInvoiceFromTreatmentPlanCommand, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(
        CreateInvoiceFromTreatmentPlanCommand cmd, CancellationToken ct)
    {
        var plan = await planReader.GetPlanItemsAsync(cmd.TreatmentPlanId, ct);
        if (plan is null)
            return Error.NotFound("TreatmentPlan.NotFound", "Treatment plan not found.");

        var (patientId, items) = plan.Value;

        if (items.Count == 0)
            return Error.Validation("TreatmentPlan.NoItems", "Treatment plan has no items to invoice.");

        var count = await repo.CountByTenantAsync(ct);
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";

        var invoice = Invoice.Create(patientId, invoiceNumber, cmd.DueDate, cmd.Notes);
        await repo.AddAsync(invoice, ct);

        foreach (var item in items)
        {
            var lineItem = InvoiceLineItem.Create(
                invoice.Id,
                item.Description,
                item.CdtCode,
                item.ToothNumber,
                1,
                item.Fee,
                item.TreatmentPlanItemId);
            await repo.AddLineItemAsync(lineItem, ct);
        }

        var created = await repo.GetByIdWithDetailsAsync(invoice.Id, ct);
        return InvoiceResponse.FromEntity(created!);
    }
}
