using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// The tenant boundary. In a clinical system this is the most load-bearing
/// guarantee in the suite: one clinic must never see another clinic's patients.
///
/// A mocked repository cannot prove this — it needs the real query filter, the
/// real resolution path and a real database.
/// </summary>
[Collection("Integration")]
public class TenantIsolationTests(DentFlowAppFactory factory)
{
    private record PatientDto(Guid Id, string FirstName, string LastName);
    private record PagedPatients(List<PatientDto> Items, int TotalCount);

    private async Task<(Guid IdA, HttpClient A, Guid IdB, HttpClient B)> TwoClinicsAsync()
    {
        var idA = await factory.SeedTenantAsync("isoclinic-a", "Isolation Clinic A");
        var idB = await factory.SeedTenantAsync("isoclinic-b", "Isolation Clinic B");

        idA.Should().NotBe(idB, "two seeded clinics must be distinct tenants");

        return (
            idA, factory.CreateClientForTenant(idA, "isoclinic-a"),
            idB, factory.CreateClientForTenant(idB, "isoclinic-b"));
    }

    private static async Task<PatientDto> CreatePatientAsync(HttpClient client, string marker)
    {
        var response = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = marker,
            lastName = "Isolation",
            email = $"{marker.ToLowerInvariant()}-{Guid.NewGuid():N}@test.com",
            phoneMobile = "+38761000777",
            smsOptIn = false,
            emailOptIn = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "the owning clinic must be able to create its own patient");

        return (await response.Content.ReadFromJsonAsync<PatientDto>())!;
    }

    [Fact]
    public async Task PatientCreatedByOneClinic_IsNotListedByAnother()
    {
        var (_, clinicA, _, clinicB) = await TwoClinicsAsync();

        var patient = await CreatePatientAsync(clinicA, "OnlyInA");

        var listedByB = await clinicB.GetFromJsonAsync<PagedPatients>(
            "/api/v1/patients?page=1&pageSize=200");

        listedByB!.Items.Should().NotContain(p => p.Id == patient.Id,
            "clinic B must not see a patient belonging to clinic A");
    }

    [Fact]
    public async Task PatientOfAnotherClinic_IsNotReachableById()
    {
        var (_, clinicA, _, clinicB) = await TwoClinicsAsync();

        var patient = await CreatePatientAsync(clinicA, "HiddenFromB");

        var response = await clinicB.GetAsync($"/api/v1/patients/{patient.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a direct id lookup must not cross the tenant boundary");
    }

    [Fact]
    public async Task WrittenRow_CarriesTheResolvedTenantId_NotEmpty()
    {
        var (idA, clinicA, _, _) = await TwoClinicsAsync();

        var patient = await CreatePatientAsync(clinicA, "TaggedWithA");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var stored = await db.Patients
            .IgnoreQueryFilters()
            .Where(p => p.Id == patient.Id)
            .Select(p => new { p.TenantId })
            .SingleAsync();

        stored.TenantId.Should().NotBe(Guid.Empty,
            "a row written with no resolved tenant is orphaned — SaveChangesAsync must not fall back to Guid.Empty");
        stored.TenantId.Should().Be(idA,
            "the row must be stamped with the clinic that created it");
    }

    [Fact]
    public async Task EachClinic_SeesOnlyItsOwnPatients()
    {
        var (_, clinicA, _, clinicB) = await TwoClinicsAsync();

        var inA = await CreatePatientAsync(clinicA, "BelongsToA");
        var inB = await CreatePatientAsync(clinicB, "BelongsToB");

        var fromA = await clinicA.GetFromJsonAsync<PagedPatients>("/api/v1/patients?page=1&pageSize=200");
        var fromB = await clinicB.GetFromJsonAsync<PagedPatients>("/api/v1/patients?page=1&pageSize=200");

        fromA!.Items.Should().Contain(p => p.Id == inA.Id);
        fromA.Items.Should().NotContain(p => p.Id == inB.Id);

        fromB!.Items.Should().Contain(p => p.Id == inB.Id);
        fromB.Items.Should().NotContain(p => p.Id == inA.Id);
    }
}
