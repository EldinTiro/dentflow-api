using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Client request 2026-11-10: a patient who books by email gets a card number at once, and
/// if they never come the number is used up with no paper card behind it. A new patient can
/// now be saved without a card; the card (number) is opened when they first come, and a
/// patient who never came can be deleted together with the missed visits.
/// </summary>
[Collection("Integration")]
public class PatientsWithoutCardTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record PatientDto(Guid Id, string? PatientNumber, string FullName);
    private record PagedDto(List<PatientDto> Items, int TotalCount);
    private record VisitDto(Guid Id);
    private record VisitPage(List<VisitDto> Items);

    private HttpClient NewClinic() =>
        factory.CreateAuthenticatedClient([Roles.Staff], $"nocard-{Guid.NewGuid():N}"[..22]);

    private static async Task<PatientDto> CreateAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/v1/patients", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PatientDto>())!;
    }

    private static Task<PatientDto> WithoutCardAsync(HttpClient client, string firstName = "Anita") =>
        CreateAsync(client, new { firstName, lastName = "Zaimović", withoutCard = true, smsOptIn = false, emailOptIn = false });

    private static Task<PatientDto> WithCardAsync(HttpClient client, string number) =>
        CreateAsync(client, new { patientNumber = number, firstName = "Kartoteka", lastName = "Pacijent", smsOptIn = false, emailOptIn = false });

    private async Task<Guid> BookAsync(HttpClient client, Guid patientId)
    {
        var admin = factory.CreateAuthenticatedClient([Roles.ClinicOwner], client.BaseAddress!.Host.Split('.')[0]);
        var dentist = await admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Card", email = $"dr-{Guid.NewGuid():N}@clinic.test",
        });
        var type = await admin.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"card-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981",
        });
        var start = DateTime.UtcNow.Date.AddDays(40).AddHours(10);
        var booked = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId,
            providerId = (await dentist.Content.ReadFromJsonAsync<IdDto>())!.Id,
            appointmentTypeId = (await type.Content.ReadFromJsonAsync<IdDto>())!.Id,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), source = "Staff",
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created, await booked.Content.ReadAsStringAsync());
        return (await booked.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    [Fact]
    public async Task APatient_CanBeSavedWithoutACard_AndSeveralOfThem()
    {
        var clinic = NewClinic();

        var anita = await WithoutCardAsync(clinic, "Anita");
        var bakir = await WithoutCardAsync(clinic, "Bakir");

        anita.PatientNumber.Should().BeNull();
        bakir.PatientNumber.Should().BeNull();
        var list = (await clinic.GetFromJsonAsync<PagedDto>("/api/v1/patients?search=Zaimovi"))!;
        list.Items.Select(p => p.PatientNumber).Should().AllSatisfy(n => n.Should().BeNull());
        list.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task WithoutCard_AndACardNumber_Together_Returns400()
    {
        var response = await NewClinic().PostAsJsonAsync("/api/v1/patients", new
        {
            patientNumber = "4300", firstName = "Both", lastName = "Ways", withoutCard = true, smsOptIn = false, emailOptIn = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpeningTheCard_GivesTheClinicsNextNumber_PatientsWithoutCardDoNotCount()
    {
        var clinic = NewClinic();
        await WithCardAsync(clinic, "4251");
        var anita = await WithoutCardAsync(clinic);
        await WithoutCardAsync(clinic, "Other");

        var opened = await clinic.PostAsJsonAsync($"/api/v1/patients/{anita.Id}/card", new { });

        opened.StatusCode.Should().Be(HttpStatusCode.OK, await opened.Content.ReadAsStringAsync());
        (await opened.Content.ReadFromJsonAsync<PatientDto>())!.PatientNumber.Should().Be("4252");
    }

    [Fact]
    public async Task OpeningTheCard_WithThePaperCardsNumber_UsesIt()
    {
        var clinic = NewClinic();
        var anita = await WithoutCardAsync(clinic);

        var opened = await clinic.PostAsJsonAsync($"/api/v1/patients/{anita.Id}/card", new { patientNumber = " 4377 " });

        (await opened.Content.ReadFromJsonAsync<PatientDto>())!.PatientNumber.Should().Be("4377");
    }

    [Fact]
    public async Task OpeningTheCard_WithANumberAnotherPatientHas_Returns409()
    {
        var clinic = NewClinic();
        await WithCardAsync(clinic, "4100");
        var anita = await WithoutCardAsync(clinic);

        var opened = await clinic.PostAsJsonAsync($"/api/v1/patients/{anita.Id}/card", new { patientNumber = "4100" });

        opened.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await opened.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("PatientNumberAlreadyExists");
    }

    [Fact]
    public async Task OpeningACardThatIsAlreadyOpen_Returns409()
    {
        var clinic = NewClinic();
        var patient = await WithCardAsync(clinic, "4101");

        var opened = await clinic.PostAsJsonAsync($"/api/v1/patients/{patient.Id}/card", new { });

        opened.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await opened.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Patient.AlreadyHasCard");
    }

    [Fact]
    public async Task OpeningAnotherClinicsPatientsCard_Returns404()
    {
        var anita = await WithoutCardAsync(NewClinic());

        (await NewClinic().PostAsJsonAsync($"/api/v1/patients/{anita.Id}/card", new { }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task APatientWhoNeverCame_IsDeleted_WithTheMissedVisit()
    {
        var clinic = NewClinic();
        var anita = await WithoutCardAsync(clinic);
        var visit = await BookAsync(clinic, anita.Id);
        (await clinic.PostAsJsonAsync($"/api/v1/appointments/{visit}/cancel", new { reason = "Nije došla" }))
            .IsSuccessStatusCode.Should().BeTrue();

        var deleted = await clinic.DeleteAsync($"/api/v1/patients/{anita.Id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent, await deleted.Content.ReadAsStringAsync());
        (await clinic.GetAsync($"/api/v1/appointments/{visit}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task APatientWithoutCard_WithAVisitAhead_IsNotDeleted()
    {
        var clinic = NewClinic();
        var anita = await WithoutCardAsync(clinic);
        await BookAsync(clinic, anita.Id);

        (await clinic.DeleteAsync($"/api/v1/patients/{anita.Id}")).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task APatientWithACard_KeepsACancelledVisitAsHistory()
    {
        var clinic = NewClinic();
        var patient = await WithCardAsync(clinic, "4102");
        var visit = await BookAsync(clinic, patient.Id);
        await clinic.PostAsJsonAsync($"/api/v1/appointments/{visit}/cancel", new { reason = "Bolestan" });

        var deleted = await clinic.DeleteAsync($"/api/v1/patients/{patient.Id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await deleted.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Patient.HasHistory");
    }
}
