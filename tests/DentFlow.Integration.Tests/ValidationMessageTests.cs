using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-26: validation reasons reached toasts as raw FluentValidation text with camel-cased
/// names in quotes ("'tooth Number' must be between 1 and 32. You entered 99.").
/// </summary>
[Collection("Integration")]
public class ValidationMessageTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private static async Task<string[]> ReasonsAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").EnumerateArray()
            .Select(e => e.GetProperty("reason").GetString()!).ToArray();

    [Fact]
    public async Task AnOutOfRangeToothNumber_ReadsLikeASentence()
    {
        var client = factory.CreateAuthenticatedClient([Roles.Dentist]);
        var patient = await client.PostAsJsonAsync("/api/v1/patients", new { firstName = "Tooth", lastName = "Msg", smsOptIn = false, emailOptIn = false });
        var patientId = (await patient.Content.ReadFromJsonAsync<IdDto>())!.Id;
        var plan = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/treatment-plans", new { title = "Plan" });
        var planId = (await plan.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await client.PostAsJsonAsync($"/api/v1/treatment-plans/{planId}/items",
            new { toothNumber = 99, description = "Filling", fee = 50m, status = "Planned" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var reasons = await ReasonsAsync(response);
        // FDI since the tooth chart (was "between 1 and 32", US numbering)
        reasons.Should().ContainSingle().Which.Should().Be("Use an FDI tooth number: 11–18, 21–28, 31–38 or 41–48.");
    }

    [Fact]
    public async Task DefaultMessages_UseReadablePropertyNames()
    {
        var response = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).PostAsJsonAsync("/api/v1/patients",
            new { firstName = "", lastName = "Name", smsOptIn = false, emailOptIn = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var reasons = await ReasonsAsync(response);
        reasons.Should().NotContain(r => r.Contains("'first Name'"));
        reasons.Should().Contain(r => r.Contains("First name"));
    }
}
