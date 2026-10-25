using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// People-merge plan B5 / D13: removing an allergy or emergency contact is a soft delete.
/// The row stays (recoverable, auditable) but no longer shows up in the patient's lists.
/// </summary>
[Collection("Integration")]
public class PatientMedicalSoftDeleteTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private async Task<(HttpClient Client, Guid PatientId)> PatientAsync()
    {
        var client = factory.CreateAuthenticatedClient([Roles.Receptionist]);
        var created = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Soft", lastName = "Delete", smsOptIn = false, emailOptIn = false
        });
        created.IsSuccessStatusCode.Should().BeTrue(await created.Content.ReadAsStringAsync());
        return (client, (await created.Content.ReadFromJsonAsync<IdDto>())!.Id);
    }

    [Fact]
    public async Task DeletingAnAllergy_KeepsTheRow_AndHidesItFromTheList()
    {
        var (client, patientId) = await PatientAsync();
        var allergy = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/allergies",
            new { allergen = "Latex", severity = "Mild" });
        var allergyId = (await allergy.Content.ReadFromJsonAsync<IdDto>())!.Id;

        (await client.DeleteAsync($"/api/v1/patients/{patientId}/allergies/{allergyId}")).IsSuccessStatusCode.Should().BeTrue();

        var list = await client.GetFromJsonAsync<List<IdDto>>($"/api/v1/patients/{patientId}/allergies");
        list.Should().NotContain(a => a.Id == allergyId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.Allergies.IgnoreQueryFilters().SingleOrDefaultAsync(a => a.Id == allergyId);
        row.Should().NotBeNull("the allergy is soft-deleted, not removed");
        row!.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeletingAnEmergencyContact_KeepsTheRow_AndHidesItFromTheList()
    {
        var (client, patientId) = await PatientAsync();
        var contact = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/emergency-contacts",
            new { name = "Brother", relationship = "Brother", phonePrimary = "+38761333444", isPrimary = false });
        var contactId = (await contact.Content.ReadFromJsonAsync<IdDto>())!.Id;

        (await client.DeleteAsync($"/api/v1/patients/{patientId}/emergency-contacts/{contactId}")).IsSuccessStatusCode.Should().BeTrue();

        var list = await client.GetFromJsonAsync<List<IdDto>>($"/api/v1/patients/{patientId}/emergency-contacts");
        list.Should().NotContain(c => c.Id == contactId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.PatientEmergencyContacts.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == contactId);
        row.Should().NotBeNull("the contact is soft-deleted, not removed");
        row!.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().NotBeNull();
    }
}
