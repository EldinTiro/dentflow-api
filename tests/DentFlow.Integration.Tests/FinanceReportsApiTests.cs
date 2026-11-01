using System.Net;
using System.Net.Http.Json;
using DentFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DentFlow.Integration.Tests;

/// <summary>
/// The owner's Finance section: payments for a period (daily cash-up, monthly revenue),
/// a period summary, and who owes money. All clinic-level money, so finance.view only.
/// Periods arrive as UTC instants computed by the client from local days (QA-008).
/// </summary>
[Collection("Integration")]
public class FinanceReportsApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record PaymentRow(Guid InvoiceId, string InvoiceNumber, Guid PatientId, string PatientName,
        decimal Amount, string Method, DateTime PaidAt, string? Reference);
    private record Summary(decimal Invoiced, decimal Outstanding, int OutstandingPatients,
        decimal Overdue, int OverduePatients);
    private record Debtor(Guid PatientId, string PatientName, string PatientNumber, decimal Balance,
        decimal OverdueBalance, int OpenInvoices, DateTime OldestOpenIssuedAt);

    // Yesterday and today: a payment can't be dated before its invoice (DT-11), and the
    // invoices here are issued now. Yesterday is within the one day of slack allowed.
    private static readonly DateTime Day1 = DateTime.UtcNow.Date.AddDays(-1).AddHours(9);
    private static readonly DateTime Day2 = DateTime.UtcNow.AddMinutes(-1);

    private async Task<(Guid TenantId, string Slug, HttpClient Owner)> NewClinicAsync()
    {
        var slug = $"fr{Guid.NewGuid():N}"[..10];
        var tenantId = await factory.SeedTenantAsync(slug, "Finance Reports Clinic");
        return (tenantId, slug, factory.CreateClientForTenant(tenantId, slug, "ClinicOwner"));
    }

    private static async Task<Guid> PatientAsync(HttpClient c, string first, string last)
    {
        var r = await c.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = first, lastName = last, email = $"{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static async Task<Guid> InvoiceAsync(HttpClient c, Guid patientId, decimal fee)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Filling", quantity = 1, unitFee = fee } }
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    /// <summary>The API only accepts a due date of today or later, so age an invoice in the database.</summary>
    private async Task MakeOverdueAsync(Guid invoiceId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var due = DateTime.UtcNow.AddDays(-3);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE invoices SET \"DueDate\" = {due} WHERE \"Id\" = {invoiceId}");
    }

    private static async Task PayAsync(HttpClient c, Guid invoiceId, decimal amount, string method, DateTime paidAt) =>
        (await c.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount, method, paidAt, reference = "R-1" })).StatusCode.Should().Be(HttpStatusCode.OK);

    private static string Range(DateTime from, DateTime to) =>
        $"from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";

    [Fact]
    public async Task Payments_ForADay_ListsOnlyThatDaysPaymentsWithPatientAndInvoice()
    {
        var (_, _, owner) = await NewClinicAsync();
        var patient = await PatientAsync(owner, "Lejla", "Cash");
        var invoice = await InvoiceAsync(owner, patient, 100m);
        await PayAsync(owner, invoice, 30m, "Cash", Day1);
        await PayAsync(owner, invoice, 20m, "Card", Day2);

        var rows = await owner.GetFromJsonAsync<List<PaymentRow>>(
            $"/api/v1/finance/payments?{Range(Day1.Date, Day1.Date.AddDays(1).AddTicks(-1))}");

        rows.Should().ContainSingle();
        rows![0].Amount.Should().Be(30m);
        rows[0].Method.Should().Be("Cash");
        rows[0].PatientName.Should().Be("Lejla Cash");
        rows[0].InvoiceId.Should().Be(invoice);
        rows[0].InvoiceNumber.Should().NotBeNullOrWhiteSpace();
        rows[0].Reference.Should().Be("R-1");
    }

    [Fact]
    public async Task Summary_ReportsInvoicedInPeriodAndCurrentOutstandingAndOverdue()
    {
        var (_, _, owner) = await NewClinicAsync();
        var a = await PatientAsync(owner, "Amra", "Owes");
        var b = await PatientAsync(owner, "Bakir", "Late");
        var paidOff = await InvoiceAsync(owner, a, 50m);
        await PayAsync(owner, paidOff, 50m, "Cash", DateTime.UtcNow.AddMinutes(-1));
        await InvoiceAsync(owner, a, 80m);                                            // open, not due
        await MakeOverdueAsync(await InvoiceAsync(owner, b, 120m));                    // open and overdue

        var now = DateTime.UtcNow;
        var summary = await owner.GetFromJsonAsync<Summary>(
            $"/api/v1/finance/summary?{Range(now.AddDays(-1), now.AddDays(1))}");

        summary!.Invoiced.Should().Be(250m);
        summary.Outstanding.Should().Be(200m);
        summary.OutstandingPatients.Should().Be(2);
        summary.Overdue.Should().Be(120m);
        summary.OverduePatients.Should().Be(1);
    }

    [Fact]
    public async Task Debtors_ListsPatientsWithABalanceOldestDebtFirst()
    {
        var (_, _, owner) = await NewClinicAsync();
        var settled = await PatientAsync(owner, "Paid", "Up");
        var first = await PatientAsync(owner, "First", "Debtor");
        var second = await PatientAsync(owner, "Second", "Debtor");
        var s = await InvoiceAsync(owner, settled, 40m);
        await PayAsync(owner, s, 40m, "Cash", DateTime.UtcNow.AddMinutes(-1));
        var f1 = await InvoiceAsync(owner, first, 100m);
        await PayAsync(owner, f1, 25m, "Card", DateTime.UtcNow.AddMinutes(-1));
        await MakeOverdueAsync(f1);
        await InvoiceAsync(owner, first, 60m);
        await InvoiceAsync(owner, second, 90m);

        var debtors = (await owner.GetFromJsonAsync<List<Debtor>>("/api/v1/finance/debtors"))!;

        debtors.Select(d => d.PatientName).Should().Equal("First Debtor", "Second Debtor");
        debtors[0].Balance.Should().Be(135m);
        debtors[0].OverdueBalance.Should().Be(75m);
        debtors[0].OpenInvoices.Should().Be(2);
        debtors[1].Balance.Should().Be(90m);
        debtors[1].OverdueBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Debtors_DoNotIncludeAnotherClinicsPatients()
    {
        var (_, _, ownerA) = await NewClinicAsync();
        var (_, _, ownerB) = await NewClinicAsync();
        var patient = await PatientAsync(ownerA, "Only", "InA");
        await InvoiceAsync(ownerA, patient, 70m);

        var debtorsB = await ownerB.GetFromJsonAsync<List<Debtor>>("/api/v1/finance/debtors");

        debtorsB.Should().BeEmpty();
    }

    [Theory]
    [InlineData("/api/v1/finance/payments")]
    [InlineData("/api/v1/finance/summary")]
    [InlineData("/api/v1/finance/debtors")]
    public async Task FinanceReports_Staff_IsForbidden(string url)
    {
        var (tenantId, slug, _) = await NewClinicAsync();
        var staff = factory.CreateClientForTenant(tenantId, slug, DentFlow.Domain.Identity.Roles.Staff);

        var response = await staff.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
