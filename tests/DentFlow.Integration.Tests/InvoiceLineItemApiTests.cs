using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>QA-004: line items on a paid invoice, and status after line-item changes.</summary>
[Collection("Integration")]
public class InvoiceLineItemApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record LineDto(Guid Id, decimal UnitFee);
    private record InvoiceDto(Guid Id, string Status, decimal SubTotal, decimal BalanceDue, List<LineDto> LineItems);

    private async Task<(HttpClient Client, InvoiceDto Invoice)> InvoiceAsync(params decimal[] fees)
    {
        var client = factory.CreateAdminClient();
        var patient = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Line", lastName = "Items",
            email = $"li-{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        var patientId = (await patient.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var created = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = fees.Select(f => new { description = "Service", quantity = 1, unitFee = f }).ToArray()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        return (client, (await created.Content.ReadFromJsonAsync<InvoiceDto>())!);
    }

    private static Task<HttpResponseMessage> PayAsync(HttpClient client, Guid invoiceId, decimal amount) =>
        client.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount, method = "Cash", paidAt = DateTime.UtcNow.AddMinutes(-1) });

    private static async Task<InvoiceDto> GetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<InvoiceDto>($"/api/v1/invoices/{id}"))!;

    [Fact]
    public async Task PartialPayment_LeavesInvoicePartiallyPaid()
    {
        var (client, invoice) = await InvoiceAsync(100m);

        var paid = await PayAsync(client, invoice.Id, 60m);

        paid.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await GetAsync(client, invoice.Id);
        after.Status.Should().Be("PartiallyPaid");
        after.BalanceDue.Should().Be(40m);
    }

    [Fact]
    public async Task AddLineItem_ToPaidInvoice_Returns409AndInvoiceStaysSettled()
    {
        var (client, invoice) = await InvoiceAsync(75m);
        (await PayAsync(client, invoice.Id, 75m)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync($"/api/v1/invoices/{invoice.Id}/line-items",
            new { description = "Extra", quantity = 1, unitFee = 10m });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var after = await GetAsync(client, invoice.Id);
        after.Status.Should().Be("Paid");
        after.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task DeleteLineItem_FromPaidInvoice_Returns409()
    {
        var (client, invoice) = await InvoiceAsync(75m, 0m);
        (await PayAsync(client, invoice.Id, 75m)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.DeleteAsync(
            $"/api/v1/invoices/{invoice.Id}/line-items/{invoice.LineItems.Single(l => l.UnitFee == 0m).Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateLineItem_OnPartiallyPaidInvoice_IsPersistedAndStatusFollows()
    {
        var (client, invoice) = await InvoiceAsync(100m);
        (await PayAsync(client, invoice.Id, 60m)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/invoices/{invoice.Id}/line-items/{invoice.LineItems[0].Id}",
            new { description = "Discounted", quantity = 1, unitFee = 60m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await GetAsync(client, invoice.Id);
        after.SubTotal.Should().Be(60m);
        after.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task DeleteLineItem_PartiallyPaid_BalanceReachesZero_StatusBecomesPaid()
    {
        var (client, invoice) = await InvoiceAsync(60m, 40m);
        (await PayAsync(client, invoice.Id, 60m)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.DeleteAsync(
            $"/api/v1/invoices/{invoice.Id}/line-items/{invoice.LineItems.Single(l => l.UnitFee == 40m).Id}");

        response.IsSuccessStatusCode.Should().BeTrue();
        (await GetAsync(client, invoice.Id)).Status.Should().Be("Paid");
    }
}
