using FastEndpoints;
using MediatR;
using DentFlow.Billing.Application;
using DentFlow.Billing.Application.Commands;
using DentFlow.Billing.Application.Queries;

namespace DentFlow.Billing.Endpoints;

// ── POST /patients/{patientId}/invoices ─────────────────────────────────────

public class InvoiceCreateRequest
{
    public DateTime? DueDate { get; init; }
    public string? Notes { get; init; }
    public List<CreateInvoiceLineItemRequest> LineItems { get; init; } = [];
}

public class InvoiceCreateEndpoint(ISender sender)
    : Endpoint<InvoiceCreateRequest, InvoiceResponse>
{
    public override void Configure()
    {
        Post("/patients/{patientId}/invoices");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff", "Receptionist");
        Version(1);
        Summary(s => s.Summary = "Create an invoice for a patient");
    }

    public override async Task HandleAsync(InvoiceCreateRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new CreateInvoiceCommand(
            Route<Guid>("patientId"), req.DueDate, req.Notes,
            req.LineItems.AsReadOnly()), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendCreatedAtAsync<InvoiceGetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

// ── GET /invoices ──────────────────────────────────────────────────────────

public class InvoiceListEndpoint(ISender sender)
    : EndpointWithoutRequest<PagedResult<InvoiceSummaryResponse>>
{
    public override void Configure()
    {
        Get("/invoices");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff", "Receptionist", "Dentist", "ReadOnly");
        Version(1);
        Summary(s => s.Summary = "List all invoices with optional filters");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        int page = Query<int>("page", isRequired: false);
        int pageSize = Query<int>("pageSize", isRequired: false);
        var query = new ListInvoicesQuery(
            Query<Guid?>("patientId", isRequired: false),
            Query<string?>("status", isRequired: false),
            Query<DateOnly?>("from", isRequired: false),
            Query<DateOnly?>("to", isRequired: false),
            page > 0 ? page : 1,
            pageSize is > 0 and < 101 ? pageSize : 20);

        var result = await sender.Send(query, ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── GET /patients/{patientId}/invoices ──────────────────────────────────────

public class InvoiceListByPatientEndpoint(ISender sender)
    : EndpointWithoutRequest<PagedResult<InvoiceSummaryResponse>>
{
    public override void Configure()
    {
        Get("/patients/{patientId}/invoices");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff", "Receptionist", "Dentist", "ReadOnly");
        Version(1);
        Summary(s => s.Summary = "List invoices for a specific patient");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        int page = Query<int>("page", isRequired: false);
        int pageSize = Query<int>("pageSize", isRequired: false);
        var query = new ListInvoicesQuery(
            Route<Guid>("patientId"),
            Query<string?>("status", isRequired: false),
            Query<DateOnly?>("from", isRequired: false),
            Query<DateOnly?>("to", isRequired: false),
            page > 0 ? page : 1,
            pageSize is > 0 and < 101 ? pageSize : 20);

        var result = await sender.Send(query, ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── GET /invoices/{id} ─────────────────────────────────────────────────────

public class InvoiceGetByIdEndpoint(ISender sender) : EndpointWithoutRequest<InvoiceResponse>
{
    public override void Configure()
    {
        Get("/invoices/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff", "Receptionist", "Dentist", "ReadOnly");
        Version(1);
        Summary(s => s.Summary = "Get invoice by ID");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(Route<Guid>("id")), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── PUT /invoices/{id} ─────────────────────────────────────────────────────

public class InvoiceUpdateRequest
{
    public DateTime? DueDate { get; init; }
    public string? Notes { get; init; }
}

public class InvoiceUpdateEndpoint(ISender sender)
    : Endpoint<InvoiceUpdateRequest, InvoiceResponse>
{
    public override void Configure()
    {
        Put("/invoices/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff");
        Version(1);
        Summary(s => s.Summary = "Update invoice metadata (due date, notes)");
    }

    public override async Task HandleAsync(InvoiceUpdateRequest req, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateInvoiceCommand(Route<Guid>("id"), req.DueDate, req.Notes), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── POST /invoices/{id}/void ───────────────────────────────────────────────

public class InvoiceVoidEndpoint(ISender sender) : EndpointWithoutRequest<InvoiceResponse>
{
    public override void Configure()
    {
        Post("/invoices/{id}/void");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff");
        Version(1);
        Summary(s => s.Summary = "Void an invoice");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new VoidInvoiceCommand(Route<Guid>("id")), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── POST /invoices/{id}/line-items ─────────────────────────────────────────

public class AddLineItemRequest
{
    public string Description { get; init; } = default!;
    public string? CdtCode { get; init; }
    public int? ToothNumber { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal UnitFee { get; init; }
    public Guid? TreatmentPlanItemId { get; init; }
}

public class AddLineItemEndpoint(ISender sender)
    : Endpoint<AddLineItemRequest, InvoiceResponse>
{
    public override void Configure()
    {
        Post("/invoices/{id}/line-items");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff");
        Version(1);
        Summary(s => s.Summary = "Add a line item to an invoice");
    }

    public override async Task HandleAsync(AddLineItemRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new AddLineItemCommand(
            Route<Guid>("id"), req.Description, req.CdtCode,
            req.ToothNumber, req.Quantity, req.UnitFee, req.TreatmentPlanItemId), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── PUT /invoices/{id}/line-items/{itemId} ─────────────────────────────────

public class UpdateLineItemRequest
{
    public string Description { get; init; } = default!;
    public string? CdtCode { get; init; }
    public int? ToothNumber { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal UnitFee { get; init; }
}

public class UpdateLineItemEndpoint(ISender sender)
    : Endpoint<UpdateLineItemRequest, InvoiceResponse>
{
    public override void Configure()
    {
        Put("/invoices/{id}/line-items/{itemId}");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff");
        Version(1);
        Summary(s => s.Summary = "Update a line item on an invoice");
    }

    public override async Task HandleAsync(UpdateLineItemRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateLineItemCommand(
            Route<Guid>("id"), Route<Guid>("itemId"),
            req.Description, req.CdtCode, req.ToothNumber, req.Quantity, req.UnitFee), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── DELETE /invoices/{id}/line-items/{itemId} ──────────────────────────────

public class DeleteLineItemEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/invoices/{id}/line-items/{itemId}");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff");
        Version(1);
        Summary(s => s.Summary = "Remove a line item from an invoice");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(
            new DeleteLineItemCommand(Route<Guid>("id"), Route<Guid>("itemId")), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendNoContentAsync(ct);
    }
}

// ── POST /treatment-plans/{planId}/invoice ─────────────────────────────────

public class CreateInvoiceFromPlanRequest
{
    public DateTime? DueDate { get; init; }
    public string? Notes { get; init; }
}

public class CreateInvoiceFromTreatmentPlanEndpoint(ISender sender)
    : Endpoint<CreateInvoiceFromPlanRequest, InvoiceResponse>
{
    public override void Configure()
    {
        Post("/treatment-plans/{planId}/invoice");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff", "Receptionist");
        Version(1);
        Summary(s => s.Summary = "Create an invoice pre-populated from all items in a treatment plan");
    }

    public override async Task HandleAsync(CreateInvoiceFromPlanRequest req, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateInvoiceFromTreatmentPlanCommand(Route<Guid>("planId"), req.DueDate, req.Notes), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendCreatedAtAsync<InvoiceGetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

// ── POST /invoices/{id}/payments ───────────────────────────────────────────

public class RecordPaymentRequest
{
    public decimal Amount { get; init; }
    public string Method { get; init; } = default!;
    public DateTime PaidAt { get; init; } = DateTime.UtcNow;
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}

public class RecordPaymentEndpoint(ISender sender)
    : Endpoint<RecordPaymentRequest, InvoiceResponse>
{
    public override void Configure()
    {
        Post("/invoices/{id}/payments");
        Roles("ClinicOwner", "ClinicAdmin", "BillingStaff", "Receptionist");
        Version(1);
        Summary(s => s.Summary = "Record a payment against an invoice");
    }

    public override async Task HandleAsync(RecordPaymentRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RecordPaymentCommand(
            Route<Guid>("id"), req.Amount, req.Method, req.PaidAt, req.Reference, req.Notes), ct);
        if (result.IsError) { foreach (var e in result.Errors) AddError(e.Description); await SendErrorsAsync(cancellation: ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

