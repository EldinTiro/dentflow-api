using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-003: the dashboard aggregates bypass the global query filters and filtered by a
/// tenant id parsed from the slug, so every tenant saw zeros.
/// </summary>
[Collection("Integration")]
public class DashboardApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record Stats(
        decimal RevenueThisMonth,
        decimal OutstandingBalance,
        int NewPatientsThisMonth,
        int AppointmentsCompletedThisWeek,
        int AppointmentsCompletedLastWeek,
        int RecallDueSoon,
        int RecallOverdue);

    private static async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Dash",
            lastName = "Board",
            email = $"dash-{Guid.NewGuid():N}@test.com",
            smsOptIn = false,
            emailOptIn = false
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static async Task<Guid> CreateInvoiceAsync(HttpClient client, Guid patientId, decimal fee)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Filling", quantity = 1, unitFee = fee } }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Fact]
    public async Task Stats_ReflectTheTenantsPaymentsBalanceAndPatients()
    {
        var tenantId = await factory.SeedTenantAsync("dashclinic", "Dashboard Clinic");
        var client = factory.CreateClientForTenant(tenantId, "dashclinic");

        var patientId = await CreatePatientAsync(client);
        var invoiceId = await CreateInvoiceAsync(client, patientId, 100m);
        var pay = await client.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments",
            new { amount = 40m, method = "Cash", paidAt = DateTime.UtcNow });
        pay.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await client.GetFromJsonAsync<Stats>("/api/v1/dashboard/stats");

        stats!.RevenueThisMonth.Should().Be(40m);
        stats.OutstandingBalance.Should().Be(60m);
        stats.NewPatientsThisMonth.Should().Be(1);
    }

    [Fact]
    public async Task Stats_DoNotIncludeAnotherTenantsData()
    {
        var idA = await factory.SeedTenantAsync("dashclinic-a", "Dashboard A");
        var idB = await factory.SeedTenantAsync("dashclinic-b", "Dashboard B");
        var clinicA = factory.CreateClientForTenant(idA, "dashclinic-a");
        var clinicB = factory.CreateClientForTenant(idB, "dashclinic-b");

        var patientId = await CreatePatientAsync(clinicA);
        await CreateInvoiceAsync(clinicA, patientId, 250m);

        var stats = await clinicB.GetFromJsonAsync<Stats>("/api/v1/dashboard/stats");

        stats!.OutstandingBalance.Should().Be(0m);
        stats.NewPatientsThisMonth.Should().Be(0);
    }
}
