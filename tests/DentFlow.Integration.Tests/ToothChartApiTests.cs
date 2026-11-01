using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// The tooth chart: findings per tooth (FDI) under /patients/{id}/teeth, and treatment plan
/// items numbered the same way.
/// </summary>
[Collection("Integration")]
public class ToothChartApiTests(DentFlowAppFactory factory)
{
    private readonly HttpClient _owner = factory.CreateAdminClient();

    private record IdDto(Guid Id);
    private record FindingDto(Guid Id, int Tooth, string Condition, string? Surfaces, string? Notes, DateTime RecordedAt);

    private async Task<Guid> PatientAsync(HttpClient client)
    {
        var r = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Zub", lastName = "Pacijent", email = $"teeth-{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Fact]
    public async Task Findings_CanBeRecordedListedAndRemoved()
    {
        var patient = await PatientAsync(_owner);
        var dentist = factory.CreateStaffClient();

        var add = await dentist.PostAsJsonAsync($"/api/v1/patients/{patient}/teeth", new { tooth = 36, condition = "Filling", surfaces = "mod", notes = "Composite" });
        add.StatusCode.Should().Be(HttpStatusCode.Created, await add.Content.ReadAsStringAsync());
        var added = (await add.Content.ReadFromJsonAsync<FindingDto>())!;
        await dentist.PostAsJsonAsync($"/api/v1/patients/{patient}/teeth", new { tooth = 11, condition = "Crown" });

        var list = (await dentist.GetFromJsonAsync<List<FindingDto>>($"/api/v1/patients/{patient}/teeth"))!;
        list.Select(f => (f.Tooth, f.Condition, f.Surfaces)).Should().Equal((11, "Crown", null), (36, "Filling", "MOD"));

        (await dentist.DeleteAsync($"/api/v1/patients/{patient}/teeth/{added.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await dentist.GetFromJsonAsync<List<FindingDto>>($"/api/v1/patients/{patient}/teeth"))!
            .Should().ContainSingle().Which.Tooth.Should().Be(11);
    }

    [Fact]
    public async Task Add_WithANonFdiTooth_Returns400WithTheCode()
    {
        var patient = await PatientAsync(_owner);

        var r = await _owner.PostAsJsonAsync($"/api/v1/patients/{patient}/teeth", new { tooth = 9, condition = "Caries" });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("ToothFinding.InvalidTooth");
    }

    // Two roles since 2026-11-02: everyone in the clinic records findings, the platform admin does not
    [Fact]
    public async Task Add_AsThePlatformAdmin_Returns403()
    {
        var patient = await PatientAsync(_owner);

        (await factory.CreatePlatformAdminClient().PostAsJsonAsync($"/api/v1/patients/{patient}/teeth", new { tooth = 36, condition = "Caries" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chart_OfAnotherClinicsPatient_Returns404()
    {
        var slug = $"teeth-{Guid.NewGuid():N}"[..20];
        var otherTenant = await factory.SeedTenantAsync(slug, "Other Clinic");
        var other = factory.CreateClientForTenant(otherTenant, slug, Roles.ClinicOwner);
        var theirs = await PatientAsync(other);
        await other.PostAsJsonAsync($"/api/v1/patients/{theirs}/teeth", new { tooth = 36, condition = "Caries" });

        (await _owner.GetAsync($"/api/v1/patients/{theirs}/teeth")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _owner.PostAsJsonAsync($"/api/v1/patients/{theirs}/teeth", new { tooth = 36, condition = "Caries" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Remove_AFindingOfAnotherPatient_Returns404()
    {
        var mine = await PatientAsync(_owner);
        var other = await PatientAsync(_owner);
        var add = await _owner.PostAsJsonAsync($"/api/v1/patients/{other}/teeth", new { tooth = 36, condition = "Caries" });
        var finding = (await add.Content.ReadFromJsonAsync<FindingDto>())!;

        (await _owner.DeleteAsync($"/api/v1/patients/{mine}/teeth/{finding.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Treatment items were numbered 1–32; the chart and the plan must speak the same FDI numbers
    [Fact]
    public async Task TreatmentPlanItems_UseFdiToothNumbers()
    {
        var patient = await PatientAsync(_owner);
        var plan = await _owner.PostAsJsonAsync($"/api/v1/patients/{patient}/treatment-plans", new { title = "Plan" });
        plan.StatusCode.Should().Be(HttpStatusCode.Created, await plan.Content.ReadAsStringAsync());
        var planId = (await plan.Content.ReadFromJsonAsync<IdDto>())!.Id;
        object Item(int tooth) => new { toothNumber = tooth, description = "Filling", fee = 50m, status = "Planned" };

        var fdi = await _owner.PostAsJsonAsync($"/api/v1/treatment-plans/{planId}/items", Item(36));
        fdi.StatusCode.Should().Be(HttpStatusCode.Created, await fdi.Content.ReadAsStringAsync());
        (await _owner.PostAsJsonAsync($"/api/v1/treatment-plans/{planId}/items", Item(9)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Editing went unchecked before: any number was accepted
        var itemId = (await fdi.Content.ReadFromJsonAsync<IdDto>())!.Id;
        (await _owner.PutAsJsonAsync($"/api/v1/treatment-plans/{planId}/items/{itemId}", Item(9)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _owner.PutAsJsonAsync($"/api/v1/treatment-plans/{planId}/items/{itemId}", Item(46)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
