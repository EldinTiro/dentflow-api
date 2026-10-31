using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// An invoice can name the appointment it bills, and an appointment is billed at most once
/// (user report: "Bill patient" stayed enabled on a visit that was already invoiced).
/// </summary>
[Collection("Integration")]
public class AppointmentInvoiceTests(DentFlowAppFactory factory)
{
    private readonly HttpClient _owner = factory.CreateAdminClient();

    private record IdDto(Guid Id);
    private record InvoiceDto(Guid Id, string InvoiceNumber, Guid? AppointmentId, string Status);
    private record PageDto(List<InvoiceDto> Items, int TotalCount);

    private async Task<Guid> PostIdAsync(string url, object body)
    {
        var r = await _owner.PostAsJsonAsync(url, body);
        r.IsSuccessStatusCode.Should().BeTrue(await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private async Task<(Guid Patient, Guid Appointment)> VisitAsync()
    {
        var patient = await PostIdAsync("/api/v1/patients", new
        {
            firstName = "Bill", lastName = "Once", email = $"bill-{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        var doctor = await PostIdAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Bill", email = $"bill-{Guid.NewGuid():N}@clinic.com", colorHex = "#3B82F6"
        });
        var type = await PostIdAsync("/api/v1/appointment-types", new { name = $"bill-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981" });
        var start = DateTime.UtcNow.Date.AddDays(30).AddHours(9);
        var appointment = await PostIdAsync("/api/v1/appointments", new
        {
            patientId = patient, providerId = doctor, appointmentTypeId = type,
            startAt = start.ToString("o"), endAt = start.AddMinutes(30).ToString("o"), chiefComplaint = "Extraction", source = "Staff"
        });
        return (patient, appointment);
    }

    private Task<HttpResponseMessage> BillAsync(Guid patient, Guid? appointment) =>
        _owner.PostAsJsonAsync($"/api/v1/patients/{patient}/invoices", new
        {
            appointmentId = appointment,
            lineItems = new[] { new { description = "Extraction", quantity = 1, unitFee = 50m } },
        });

    [Fact]
    public async Task Invoice_RemembersItsAppointment_AndCanBeFoundByIt()
    {
        var (patient, appointment) = await VisitAsync();

        var r = await BillAsync(patient, appointment);

        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        (await r.Content.ReadFromJsonAsync<InvoiceDto>())!.AppointmentId.Should().Be(appointment);
        var page = (await _owner.GetFromJsonAsync<PageDto>($"/api/v1/patients/{patient}/invoices?appointmentId={appointment}"))!;
        page.Items.Should().ContainSingle().Which.AppointmentId.Should().Be(appointment);
    }

    [Fact]
    public async Task Invoice_ForAnAppointmentAlreadyBilled_Returns409()
    {
        var (patient, appointment) = await VisitAsync();
        (await BillAsync(patient, appointment)).StatusCode.Should().Be(HttpStatusCode.Created);

        var again = await BillAsync(patient, appointment);

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await again.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Invoice.AppointmentAlreadyBilled");
    }

    // A voided invoice no longer bills anything, so the visit can be billed again
    [Fact]
    public async Task Invoice_AfterTheFirstWasVoided_IsAllowed()
    {
        var (patient, appointment) = await VisitAsync();
        var first = (await (await BillAsync(patient, appointment)).Content.ReadFromJsonAsync<InvoiceDto>())!;
        (await _owner.PostAsync($"/api/v1/invoices/{first.Id}/void", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await BillAsync(patient, appointment)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Invoice_ForAnotherPatientsAppointment_Returns404()
    {
        var (_, theirAppointment) = await VisitAsync();
        var (me, _) = await VisitAsync();

        var r = await BillAsync(me, theirAppointment);

        r.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Invoice.AppointmentNotFound");
    }

    [Fact]
    public async Task Invoice_WithoutAnAppointment_StillWorks()
    {
        var (patient, _) = await VisitAsync();

        var r = await BillAsync(patient, null);

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        (await r.Content.ReadFromJsonAsync<InvoiceDto>())!.AppointmentId.Should().BeNull();
    }
}
