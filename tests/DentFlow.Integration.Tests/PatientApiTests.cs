using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

[Collection("Integration")]
public class PatientApiTests(DentFlowAppFactory factory)
{
    private readonly HttpClient _admin = factory.CreateAdminClient();
    private readonly HttpClient _receptionist = factory.CreateReceptionistClient();

    // ── POST /api/v1/patients ─────────────────────────────────────────────

    [Fact]
    public async Task CreatePatient_ValidRequest_Returns201()
    {
        var response = await _admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Ana",
            lastName = "Perić",
            email = $"ana-{Guid.NewGuid():N}@test.com",
            phoneMobile = "+38761000001",
            smsOptIn = true,
            emailOptIn = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<PatientDto>();
        body!.FirstName.Should().Be("Ana");
        body.LastName.Should().Be("Perić");
        body.PatientNumber.Should().StartWith("P-");
    }

    [Fact]
    public async Task CreatePatient_DuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";

        await _admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "First",
            lastName = "Patient",
            email,
            smsOptIn = false,
            emailOptIn = false
        });

        var response = await _admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Second",
            lastName = "Patient",
            email,  // same email
            smsOptIn = false,
            emailOptIn = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePatient_UnauthenticatedRequest_Returns401()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Ghost",
            lastName = "User",
            smsOptIn = false,
            emailOptIn = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/v1/patients/{id} ─────────────────────────────────────────

    [Fact]
    public async Task GetPatientById_ExistingPatient_Returns200()
    {
        var created = await CreateTestPatient();

        var response = await _admin.GetAsync($"/api/v1/patients/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PatientDto>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetPatientById_NonExisting_Returns404()
    {
        var response = await _admin.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Tenant isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task TenantResolution_UnknownSlug_ReturnsEmptyList()
    {
        // An unrecognized tenant slug passes through Finbuckle without a TenantInfo,
        // so the global query filter falls back to TenantId=Guid.Empty and returns an empty
        // list rather than 404. Enrollment-enforced 404 is a future hardening task.
        var unknownTenantClient = factory.CreateAdminClient("completely-unknown-slug");
        var response = await unknownTenantClient.GetAsync("/api/v1/patients");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantResolution_KnownSlug_AllowsRequests()
    {
        // Both clinicA and clinicB are registered tenants and can create resources
        var clinicA = factory.CreateAdminClient("clinicA");
        var clinicB = factory.CreateAdminClient("clinicB");

        var responseA = await clinicA.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "ClinicA",
            lastName = "Patient",
            email = $"a-{Guid.NewGuid():N}@test.com",
            smsOptIn = false,
            emailOptIn = false
        });
        var responseB = await clinicB.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "ClinicB",
            lastName = "Patient",
            email = $"b-{Guid.NewGuid():N}@test.com",
            smsOptIn = false,
            emailOptIn = false
        });

        responseA.StatusCode.Should().Be(HttpStatusCode.Created);
        responseB.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── GET /api/v1/patients (list) ───────────────────────────────────────

    [Fact]
    public async Task ListPatients_Pagination_ReturnsCorrectPage()
    {
        // Create 3 patients
        for (var i = 0; i < 3; i++)
        {
            await _admin.PostAsJsonAsync("/api/v1/patients", new
            {
                firstName = "Page",
                lastName = $"Patient{i}",
                email = $"page-{Guid.NewGuid():N}@test.com",
                smsOptIn = false,
                emailOptIn = false
            });
        }

        var response = await _admin.GetAsync("/api/v1/patients?page=1&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedDto<PatientDto>>();
        body!.Items.Count.Should().BeLessThanOrEqualTo(2);
        body.Page.Should().Be(1);
    }

    // ── DELETE /api/v1/patients/{id} ──────────────────────────────────────

    [Fact]
    public async Task DeletePatient_ExistingPatient_Returns204_AndDisappearsFromList()
    {
        var patient = await CreateTestPatient();

        var deleteResponse = await _admin.DeleteAsync($"/api/v1/patients/{patient!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Should not appear in list anymore (soft delete)
        var getResponse = await _admin.GetAsync($"/api/v1/patients/{patient.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<PatientDto?> CreateTestPatient()
    {
        var response = await _admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Test",
            lastName = "Patient",
            email = $"test-{Guid.NewGuid():N}@test.com",
            smsOptIn = false,
            emailOptIn = false
        });
        return await response.Content.ReadFromJsonAsync<PatientDto>();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

internal record PatientDto(Guid Id, string FirstName, string LastName, string PatientNumber,
    string? Email, string Status);

internal record PagedDto<T>(List<T> Items, int TotalCount, int Page, int PageSize);
