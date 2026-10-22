using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-005: most endpoints called SendErrorsAsync() without adding the ErrorOr errors,
/// so clients got 400 with an empty errors[] — and NotFound/Conflict also came out as 400.
/// </summary>
[Collection("Integration")]
public class ErrorResponseTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private static async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Err",
            lastName = "Shape",
            email = $"err-{Guid.NewGuid():N}@test.com",
            smsOptIn = false,
            emailOptIn = false
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static async Task<List<string>> ReasonsAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("reason").GetString()!)
            .ToList();
    }

    [Fact]
    public async Task TreatmentPlanItem_InvalidFee_Returns400WithTheValidationReason()
    {
        var client = factory.CreateAdminClient();
        var patientId = await CreatePatientAsync(client);
        var plan = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/treatment-plans",
            new { title = "Plan", notes = (string?)null });
        plan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await plan.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await client.PostAsJsonAsync($"/api/v1/treatment-plans/{planId}/items", new
        {
            toothNumber = 11,
            description = "Filling",
            fee = -50m,
            status = "Planned"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var reasons = await ReasonsAsync(response);
        reasons.Should().ContainSingle(r => r.Contains("fee", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MissingTreatmentPlan_Returns404()
    {
        var client = factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/treatment-plans/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvoiceOfAnotherClinic_Returns404()
    {
        var idA = await factory.SeedTenantAsync("errclinic-a", "Err A");
        var idB = await factory.SeedTenantAsync("errclinic-b", "Err B");
        var clinicA = factory.CreateClientForTenant(idA, "errclinic-a");
        var clinicB = factory.CreateClientForTenant(idB, "errclinic-b");

        var patientId = await CreatePatientAsync(clinicA);
        var invoice = await clinicA.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Exam", quantity = 1, unitFee = 10m } }
        });
        var invoiceId = (await invoice.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await clinicB.GetAsync($"/api/v1/invoices/{invoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
