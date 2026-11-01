using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Clinic-level money (revenue, outstanding totals, the full invoice ledger) is for the
/// clinic owner only; everyone at the desk still sees and collects a
/// patient's own bill. Before this, /dashboard/stats sent revenue to every clinic role
/// and /invoices listed the whole ledger to dentists and receptionists.
/// </summary>
[Collection("Integration")]
public class FinanceAccessTests(DentFlowAppFactory factory)
{
    private const string Slug = "financeclinic";
    private record IdDto(Guid Id);
    private record Finance(decimal RevenueThisMonth, decimal OutstandingBalance);

    private async Task<(Guid TenantId, Guid PatientId, Guid InvoiceId)> SeedBillAsync()
    {
        var tenantId = await factory.SeedTenantAsync(Slug, "Finance Clinic");
        var owner = factory.CreateClientForTenant(tenantId, Slug, "ClinicOwner");

        var patient = await owner.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Fin", lastName = "Ance", email = $"fin-{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        patient.StatusCode.Should().Be(HttpStatusCode.Created);
        var patientId = (await patient.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var invoice = await owner.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Filling", quantity = 1, unitFee = 100m } }
        });
        invoice.StatusCode.Should().Be(HttpStatusCode.Created);
        return (tenantId, patientId, (await invoice.Content.ReadFromJsonAsync<IdDto>())!.Id);
    }

    [Fact]
    public async Task DashboardFinance_Owner_SeesRevenueAndOutstanding()
    {
        var (tenantId, _, invoiceId) = await SeedBillAsync();
        var owner = factory.CreateClientForTenant(tenantId, Slug, "ClinicOwner");
        (await owner.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount = 30m, method = "Cash", paidAt = DateTime.UtcNow })).StatusCode.Should().Be(HttpStatusCode.OK);

        var finance = await owner.GetFromJsonAsync<Finance>("/api/v1/dashboard/finance");

        finance!.RevenueThisMonth.Should().BeGreaterThanOrEqualTo(30m);
        finance.OutstandingBalance.Should().BeGreaterThanOrEqualTo(70m);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    public async Task DashboardFinance_StaffWithoutFinanceView_IsForbidden(string role)
    {
        var (tenantId, _, _) = await SeedBillAsync();
        var client = factory.CreateClientForTenant(tenantId, Slug, role);

        var response = await client.GetAsync("/api/v1/dashboard/finance");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    public async Task DashboardStats_StaffWithoutFinanceView_GetsNoMoneyFigures(string role)
    {
        var (tenantId, _, _) = await SeedBillAsync();
        var client = factory.CreateClientForTenant(tenantId, Slug, role);

        var response = await client.GetAsync("/api/v1/dashboard/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("revenue").And.NotContainEquivalentOf("outstanding");
    }

    [Theory]
    [InlineData(Roles.Staff)]
    public async Task InvoiceLedger_StaffWithoutFinanceView_IsForbidden(string role)
    {
        var (tenantId, _, _) = await SeedBillAsync();
        var client = factory.CreateClientForTenant(tenantId, Slug, role);

        var response = await client.GetAsync("/api/v1/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("ClinicOwner")]
    public async Task InvoiceLedger_FinanceRoles_CanList(string role)
    {
        var (tenantId, _, _) = await SeedBillAsync();
        var client = factory.CreateClientForTenant(tenantId, Slug, role);

        var response = await client.GetAsync("/api/v1/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    public async Task PatientInvoices_DeskAndClinicalStaff_CanStillSeeAPatientsBill(string role)
    {
        var (tenantId, patientId, invoiceId) = await SeedBillAsync();
        var client = factory.CreateClientForTenant(tenantId, Slug, role);

        (await client.GetAsync($"/api/v1/patients/{patientId}/invoices")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/invoices/{invoiceId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Payment_Receptionist_CanStillTakeAPayment()
    {
        var (tenantId, _, invoiceId) = await SeedBillAsync();
        var receptionist = factory.CreateClientForTenant(tenantId, Slug, Roles.Staff);

        var response = await receptionist.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount = 10m, method = "Cash", paidAt = DateTime.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
