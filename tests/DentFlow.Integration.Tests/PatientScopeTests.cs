using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-01 / DT-02: a write that names a patient must find that patient in the caller's own
/// clinic. Another clinic's patient and an id that doesn't exist both answer 404, and no
/// row is stored (before, they were created under the caller's tenant or ended in 500).
/// </summary>
[Collection("Integration")]
public class PatientScopeTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    private const string Theirs = "scope-pink";

    /// <summary>A patient of the default test clinic, with nothing attached yet.</summary>
    private async Task<Guid> OurPatientAsync()
    {
        var created = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Scope", lastName = "Target", smsOptIn = false, emailOptIn = false
        });
        created.IsSuccessStatusCode.Should().BeTrue(await created.Content.ReadAsStringAsync());
        return (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    public static TheoryData<string> Writes =>
    [
        "allergy", "emergency-contact", "medical-history", "treatment-plan", "invoice",
    ];

    private static Task<HttpResponseMessage> WriteAsync(HttpClient client, string kind, Guid patientId) => kind switch
    {
        "allergy" => client.PostAsJsonAsync($"/api/v1/patients/{patientId}/allergies", new { allergen = "Latex", severity = "Mild" }),
        "emergency-contact" => client.PostAsJsonAsync($"/api/v1/patients/{patientId}/emergency-contacts",
            new { name = "Cross", relationship = "Brother", phonePrimary = "+38761000111", isPrimary = false }),
        "medical-history" => client.PutAsJsonAsync($"/api/v1/patients/{patientId}/medical-history", new { isSmoker = true }),
        "treatment-plan" => client.PostAsJsonAsync($"/api/v1/patients/{patientId}/treatment-plans", new { title = "Cross plan" }),
        "invoice" => client.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Check-up", quantity = 1, unitFee = 10m } },
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private async Task<int> RowsForAsync(Guid patientId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Allergies.IgnoreQueryFilters().CountAsync(a => a.PatientId == patientId)
            + await db.PatientEmergencyContacts.IgnoreQueryFilters().CountAsync(c => c.PatientId == patientId)
            + await db.MedicalHistories.IgnoreQueryFilters().CountAsync(m => m.PatientId == patientId)
            + await db.TreatmentPlans.IgnoreQueryFilters().CountAsync(p => p.PatientId == patientId)
            + await db.Invoices.IgnoreQueryFilters().CountAsync(i => i.PatientId == patientId)
            + await db.Appointments.IgnoreQueryFilters().CountAsync(a => a.PatientId == patientId);
    }

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task AnotherClinic_CannotAttachRecordsToOurPatient(string kind)
    {
        var patientId = await OurPatientAsync();
        var other = factory.CreateAuthenticatedClient([Roles.ClinicOwner], Theirs);

        var response = await WriteAsync(other, kind, patientId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        (await RowsForAsync(patientId)).Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task AnUnknownPatient_Returns404_NotACreatedRowOr500(string kind)
    {
        var unknown = Guid.NewGuid();

        var response = await WriteAsync(factory.CreateAuthenticatedClient([Roles.ClinicOwner]), kind, unknown);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Patient.NotFound");
        (await RowsForAsync(unknown)).Should().Be(0);
    }

    [Fact]
    public async Task AnotherClinic_CannotReadOurPatientsMedicalData()
    {
        var patientId = await OurPatientAsync();
        var other = factory.CreateAuthenticatedClient([Roles.ClinicOwner], Theirs);

        foreach (var path in new[] { "allergies", "emergency-contacts", "medical-history", "treatment-plans" })
            (await other.GetAsync($"/api/v1/patients/{patientId}/{path}")).StatusCode
                .Should().Be(HttpStatusCode.NotFound, path);
    }

    private async Task<(Guid ProviderId, Guid TypeId)> BookingSetupAsync(HttpClient client)
    {
        var staff = await client.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Scope", email = $"dr-scope-{Guid.NewGuid():N}@clinic.test",
        });
        var type = await client.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"scope-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981",
        });
        return ((await staff.Content.ReadFromJsonAsync<IdDto>())!.Id, (await type.Content.ReadFromJsonAsync<IdDto>())!.Id);
    }

    private static Task<HttpResponseMessage> BookAsync(HttpClient client, Guid patientId, Guid providerId, Guid typeId)
    {
        var start = DateTime.UtcNow.Date.AddDays(14).AddHours(9);
        return client.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId, providerId, appointmentTypeId = typeId,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), source = "Staff",
        });
    }

    [Fact]
    public async Task Booking_AnotherClinicsPatient_Returns404()
    {
        var patientId = await OurPatientAsync();
        var other = factory.CreateAuthenticatedClient([Roles.ClinicOwner], Theirs);
        var (providerId, typeId) = await BookingSetupAsync(other);

        var response = await BookAsync(other, patientId, providerId, typeId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        (await RowsForAsync(patientId)).Should().Be(0);
    }

    [Fact]
    public async Task Booking_AnUnknownOrForeignProvider_Returns404()
    {
        var ours = factory.CreateAuthenticatedClient([Roles.ClinicOwner]);
        var patientId = await OurPatientAsync();
        var (_, typeId) = await BookingSetupAsync(ours);
        var (foreignProvider, _) = await BookingSetupAsync(factory.CreateAuthenticatedClient([Roles.ClinicOwner], Theirs));

        foreach (var providerId in new[] { Guid.NewGuid(), foreignProvider })
        {
            var response = await BookAsync(ours, patientId, providerId, typeId);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
            (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Staff.NotFound");
        }
        (await RowsForAsync(patientId)).Should().Be(0);
    }
}
