using DentFlow.Application.Common.Interfaces;
using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Billing.Application.Interfaces;
using DentFlow.Billing.Domain;

namespace DentFlow.Billing.Application.Queries;

// ── Get Invoice By Id ───────────────────────────────────────────────────────

public record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<ErrorOr<InvoiceResponse>>;

public class GetInvoiceByIdQueryHandler(IInvoiceRepository repo)
    : IRequestHandler<GetInvoiceByIdQuery, ErrorOr<InvoiceResponse>>
{
    public async Task<ErrorOr<InvoiceResponse>> Handle(GetInvoiceByIdQuery query, CancellationToken ct)
    {
        var invoice = await repo.GetByIdWithDetailsAsync(query.InvoiceId, ct);
        if (invoice is null) return BillingErrors.InvoiceNotFound;
        return InvoiceResponse.FromEntity(invoice);
    }
}

// ── List Invoices ───────────────────────────────────────────────────────────

public record ListInvoicesQuery(
    Guid? PatientId,
    string? Status,
    DateTime? From, // UTC, inclusive
    DateTime? To,   // UTC, inclusive
    int Page = 1,
    int PageSize = 20)
    : IRequest<ErrorOr<PagedResult<InvoiceSummaryResponse>>>;

public class ListInvoicesQueryHandler(IInvoiceRepository repo, IPatientDirectory patients)
    : IRequestHandler<ListInvoicesQuery, ErrorOr<PagedResult<InvoiceSummaryResponse>>>
{
    public async Task<ErrorOr<PagedResult<InvoiceSummaryResponse>>> Handle(
        ListInvoicesQuery query, CancellationToken ct)
    {
        InvoiceStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<InvoiceStatus>(query.Status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var (items, total) = await repo.ListAsync(
            query.PatientId, statusFilter, query.From, query.To,
            query.Page, query.PageSize, ct);

        // The list could not tell invoices apart without opening them (QA-025).
        var names = await patients.GetNamesAsync(items.Select(i => i.PatientId), ct);
        var now = DateTime.UtcNow;
        var responses = items
            .Select(i => InvoiceSummaryResponse.FromEntity(i, names.GetValueOrDefault(i.PatientId), now))
            .ToList().AsReadOnly();
        return new PagedResult<InvoiceSummaryResponse>(responses, total, query.Page, query.PageSize);
    }
}

public class ListInvoicesQueryValidator : AbstractValidator<ListInvoicesQuery>
{
    public ListInvoicesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
