using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-015: search ignored phone numbers, full names ("Selma Aganović") and needed the
/// exact diacritics — a patient without an email was unfindable by "aganovic".
/// Each test runs in its own clinic so results are exact.
/// </summary>
[Collection("Integration")]
public class PatientSearchTests(DentFlowAppFactory factory)
{
    private record PatientDto(Guid Id, string FirstName, string LastName);
    private record Paged(List<PatientDto> Items, int TotalCount);

    private async Task<HttpClient> ClinicWithSelmaAsync()
    {
        var slug = $"search-{Guid.NewGuid():N}"[..20];
        var client = factory.CreateClientForTenant(await factory.SeedTenantAsync(slug, "Search Clinic"), slug);
        foreach (var (first, last, phone) in new[]
                 {
                     ("Selma", "Aganović", "+38761542123"),
                     ("Amar", "Hodžić", "+38762999888"),
                     ("Haris", "Đurić", "+38763111222"),
                     ("Lejla", "Djulić", "+38763333444"),
                 })
        {
            var r = await client.PostAsJsonAsync("/api/v1/patients", new
            {
                firstName = first, lastName = last, phoneMobile = phone, smsOptIn = false, emailOptIn = false
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        return client;
    }

    [Theory]
    [InlineData("+38761542")]       // international prefix
    [InlineData("061542")]          // local format with trunk 0
    [InlineData("61 542 123")]      // spaced
    [InlineData("Selma Aganović")]  // full name
    [InlineData("aganovic selma")]  // no diacritics, any order
    [InlineData("AGANOVIĆ")]        // case
    public async Task List_FindsSelma(string query)
    {
        var client = await ClinicWithSelmaAsync();

        var result = await client.GetFromJsonAsync<Paged>($"/api/v1/patients?search={Uri.EscapeDataString(query)}");

        result!.Items.Should().ContainSingle().Which.FirstName.Should().Be("Selma");
    }

    [Theory]
    [InlineData("djuric", "Haris")]   // Đ typed as dj
    [InlineData("duric", "Haris")]    // Đ typed as d
    [InlineData("Đurić", "Haris")]
    [InlineData("djulic", "Lejla")]   // stored with dj
    [InlineData("Djulić", "Lejla")]
    public async Task List_TreatsDjAndĐAlike(string query, string expected)
    {
        var client = await ClinicWithSelmaAsync();

        var result = await client.GetFromJsonAsync<Paged>($"/api/v1/patients?search={Uri.EscapeDataString(query)}");

        result!.Items.Should().ContainSingle().Which.FirstName.Should().Be(expected);
    }

    [Fact]
    public async Task List_FullNameMustMatchEveryWord()
    {
        var client = await ClinicWithSelmaAsync();

        var result = await client.GetFromJsonAsync<Paged>("/api/v1/patients?search=Selma%20Hodzic");

        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Autocomplete_FindsByPhone()
    {
        var client = await ClinicWithSelmaAsync();

        var response = await client.GetAsync("/api/v1/patients/search?q=062999");

        (await response.Content.ReadAsStringAsync()).Should().Contain("Amar");
    }
}
