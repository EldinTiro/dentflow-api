using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Patients.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Patients redesign: the list says what the desk needs per patient (last visit, next
/// appointment, balance, allergies), and a patient has a timeline.
/// </summary>
[Collection("Integration")]
public class PatientInsightsApiTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record NextDto(Guid Id, DateTime StartAt, Guid ProviderId);
    private record InsightsDto(DateTime? LastVisitAt, NextDto? NextAppointment, decimal BalanceDue, int AllergyCount, string? SeverestAllergy, int VisitCount, int DocumentCount, DateTime? FirstVisitAt);
    private record PatientDto(Guid Id, string FirstName, InsightsDto? Insights);
    private record PageDto(List<PatientDto> Items, int TotalCount);
    private record TimelineDto(DateTime At, string Kind, Guid RefId, string? Status, string? Label, decimal? Amount);

    private sealed record Clinic(HttpClient Owner, Guid Lejla, Guid Karlo, Guid Miro, Guid Ana);

    private static async Task<Guid> PatientAsync(HttpClient c, string firstName)
    {
        var r = await c.PostAsJsonAsync("/api/v1/patients", new { firstName, lastName = "Test", smsOptIn = false, emailOptIn = false });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    /// <summary>Lejla is allergic, Karlo owes 60 KM, Miro hasn't been in for 8 months, Ana comes today.</summary>
    private async Task<Clinic> ClinicAsync()
    {
        var slug = $"ins-{Guid.NewGuid():N}"[..20];
        var tenant = await factory.SeedTenantAsync(slug, "Insights Clinic");
        var owner = factory.CreateClientForTenant(tenant, slug, Roles.ClinicOwner);

        var lejla = await PatientAsync(owner, "Lejla");
        (await owner.PostAsJsonAsync($"/api/v1/patients/{lejla}/allergies", new { allergen = "Penicillin", severity = "Severe" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var karlo = await PatientAsync(owner, "Karlo");
        var invoice = await owner.PostAsJsonAsync($"/api/v1/patients/{karlo}/invoices", new
        {
            lineItems = new[] { new { description = "Filling", quantity = 1, unitFee = 80m } },
        });
        invoice.StatusCode.Should().Be(HttpStatusCode.Created, await invoice.Content.ReadAsStringAsync());
        var invoiceId = (await invoice.Content.ReadFromJsonAsync<IdDto>())!.Id;
        (await owner.PostAsJsonAsync($"/api/v1/invoices/{invoiceId}/payments", new { amount = 20m, method = "Cash" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var miro = await PatientAsync(owner, "Miro");
        await AppointmentSeed.BookAsync(factory, miro, DateTime.UtcNow.AddMonths(-8), status: "Completed");

        var ana = await PatientAsync(owner, "Ana");
        await AppointmentSeed.BookAsync(factory, ana, DateTime.UtcNow.AddHours(1));

        return new Clinic(owner, lejla, karlo, miro, ana);
    }

    [Fact]
    public async Task List_SaysWhatTheDeskNeedsPerPatient()
    {
        var c = await ClinicAsync();

        var page = (await c.Owner.GetFromJsonAsync<PageDto>("/api/v1/patients?pageSize=50"))!;
        var byId = page.Items.ToDictionary(p => p.Id, p => p.Insights!);

        byId[c.Lejla].AllergyCount.Should().Be(1);
        byId[c.Lejla].SeverestAllergy.Should().Be("Severe");
        byId[c.Karlo].BalanceDue.Should().Be(60m);
        byId[c.Miro].LastVisitAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(-8), TimeSpan.FromMinutes(5));
        byId[c.Miro].NextAppointment.Should().BeNull();
        byId[c.Ana].NextAppointment!.StartAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromMinutes(5));
        byId[c.Ana].LastVisitAt.Should().BeNull();
    }

    // The preview shows when the patient first came, how often they came and how many files they have
    [Fact]
    public async Task List_CountsVisitsAndFiles_AndKnowsTheFirstVisit()
    {
        var c = await ClinicAsync();
        await AppointmentSeed.BookAsync(factory, c.Miro, DateTime.UtcNow.AddMonths(-3), status: "Completed");
        await AppointmentSeed.BookAsync(factory, c.Miro, DateTime.UtcNow.AddMonths(-2), status: "Cancelled");
        await AddDocumentAsync(c.Miro);
        await AddDocumentAsync(c.Miro);

        var page = (await c.Owner.GetFromJsonAsync<PageDto>("/api/v1/patients?pageSize=50"))!;
        var byId = page.Items.ToDictionary(p => p.Id, p => p.Insights!);

        byId[c.Miro].VisitCount.Should().Be(2);
        byId[c.Miro].FirstVisitAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(-8), TimeSpan.FromMinutes(5));
        byId[c.Miro].LastVisitAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(-3), TimeSpan.FromMinutes(5));
        byId[c.Ana].FirstVisitAt.Should().BeNull();
        byId[c.Miro].DocumentCount.Should().Be(2);
        byId[c.Ana].VisitCount.Should().Be(0);
        byId[c.Ana].DocumentCount.Should().Be(0);
    }

    private async Task AddDocumentAsync(Guid patientId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var patient = await db.Set<Patient>().IgnoreQueryFilters().SingleAsync(p => p.Id == patientId);
        var document = PatientDocument.Create(patientId, "xray.png", "image/png", 10, $"k/{Guid.NewGuid()}", "XRay", null, Guid.NewGuid());
        document.SetTenant(patient.TenantId);
        db.Add(document);
        await db.SaveChangesAsync();
    }

    // A voided invoice is not owed (DT-03)
    [Fact]
    public async Task Balance_LeavesOutVoidedInvoices()
    {
        var c = await ClinicAsync();
        var invoice = await c.Owner.PostAsJsonAsync($"/api/v1/patients/{c.Lejla}/invoices", new
        {
            lineItems = new[] { new { description = "Whitening", quantity = 1, unitFee = 200m } },
        });
        var id = (await invoice.Content.ReadFromJsonAsync<IdDto>())!.Id;
        (await c.Owner.PostAsync($"/api/v1/invoices/{id}/void", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var page = (await c.Owner.GetFromJsonAsync<PageDto>("/api/v1/patients?pageSize=50"))!;

        page.Items.Single(p => p.Id == c.Lejla).Insights!.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task Timeline_ListsWhatHappened_NewestFirst()
    {
        var c = await ClinicAsync();

        var items = (await c.Owner.GetFromJsonAsync<List<TimelineDto>>($"/api/v1/patients/{c.Karlo}/timeline"))!;

        items.Select(i => i.Kind).Should().Contain(["invoice", "payment"]);
        items.Single(i => i.Kind == "payment").Amount.Should().Be(20m);
        items.Single(i => i.Kind == "invoice").Amount.Should().Be(80m);
        items.Select(i => i.At).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Timeline_ShowsVisitsAndToothFindings()
    {
        var c = await ClinicAsync();
        (await c.Owner.PostAsJsonAsync($"/api/v1/patients/{c.Miro}/teeth", new { tooth = 36, condition = "Filling", surfaces = "O" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var items = (await c.Owner.GetFromJsonAsync<List<TimelineDto>>($"/api/v1/patients/{c.Miro}/timeline"))!;

        items.Should().Contain(i => i.Kind == "appointment" && i.Status == "Completed");
        items.Should().Contain(i => i.Kind == "finding" && i.Label == "36 Filling O");
    }

    [Fact]
    public async Task Timeline_OfAnotherClinicsPatient_Returns404()
    {
        var mine = await ClinicAsync();
        var theirs = await ClinicAsync();

        (await mine.Owner.GetAsync($"/api/v1/patients/{theirs.Karlo}/timeline")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // The patient record shows the same facts at the top (patients redesign C)
    [Fact]
    public async Task GetById_CarriesTheSameFacts()
    {
        var c = await ClinicAsync();

        var karlo = (await c.Owner.GetFromJsonAsync<PatientDto>($"/api/v1/patients/{c.Karlo}"))!;

        karlo.Insights!.BalanceDue.Should().Be(60m);
    }
}
