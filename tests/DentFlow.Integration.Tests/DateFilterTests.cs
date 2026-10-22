using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA-008: list filters took a calendar date and compared it with the UTC date, so for
/// a clinic on UTC+1/+2 anything in the first hours after local midnight fell on the
/// previous day. The client now sends the exact UTC bounds of the local day.
/// </summary>
[Collection("Integration")]
public class DateFilterTests(DentFlowAppFactory factory)
{
    private readonly HttpClient _admin = factory.CreateAdminClient();

    private record IdDto(Guid Id);
    private record Paged(List<IdDto> Items, int TotalCount);

    private async Task<Guid> CreateAsync(string url, object body)
    {
        var response = await _admin.PostAsJsonAsync(url, body);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private Task<Guid> PatientAsync() => CreateAsync("/api/v1/patients", new
    {
        firstName = "Date", lastName = "Filter",
        email = $"df-{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
    });

    [Fact]
    public async Task Appointments_FilteredByTheLocalDaysUtcBounds_IncludeJustAfterMidnight()
    {
        var patientId = await PatientAsync();
        var providerId = await CreateAsync("/api/v1/staff", new
        {
            staffType = "Dentist", firstName = "Dr", lastName = "Midnight",
            email = $"dr-{Guid.NewGuid():N}@clinic.com", colorHex = "#3B82F6"
        });
        var typeId = await CreateAsync("/api/v1/appointment-types", new
        {
            name = $"df-{Guid.NewGuid():N}", defaultDurationMinutes = 30, colorHex = "#10B981"
        });

        // 11 Mar 2027 00:30 in Sarajevo (CET, UTC+1)
        await CreateAsync("/api/v1/appointments", new
        {
            patientId, providerId, appointmentTypeId = typeId,
            startAt = "2027-03-10T23:30:00Z", endAt = "2027-03-11T00:00:00Z",
            chiefComplaint = "Late", source = "Staff"
        });

        var response = await _admin.GetAsync(
            $"/api/v1/appointments?patientId={patientId}" +
            "&dateFrom=2027-03-10T23:00:00.000Z&dateTo=2027-03-11T22:59:59.999Z");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Paged>())!.TotalCount.Should().Be(1);

        // QA-033: the client loaded 500 patients to put a name on each appointment
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"patientName\":\"Date Filter\"");
    }

    [Fact]
    public async Task Invoices_FilteredByInstantBounds_IncludeAnInvoiceIssuedInside()
    {
        var patientId = await PatientAsync();
        await CreateAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Exam", quantity = 1, unitFee = 10m } }
        });
        var from = DateTime.UtcNow.AddHours(-1).ToString("o");
        var to = DateTime.UtcNow.AddHours(1).ToString("o");

        var response = await _admin.GetAsync(
            $"/api/v1/patients/{patientId}/invoices?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Paged>())!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Invoices_FilteredByInstantBounds_ExcludeAnInvoiceIssuedBeforeTheBound()
    {
        var patientId = await PatientAsync();
        await CreateAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Exam", quantity = 1, unitFee = 10m } }
        });
        var from = DateTime.UtcNow.AddMinutes(5).ToString("o");
        var to = DateTime.UtcNow.AddHours(1).ToString("o");

        var response = await _admin.GetAsync(
            $"/api/v1/patients/{patientId}/invoices?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Paged>())!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Invoices_PlainDateFilter_StillWorks()
    {
        var patientId = await PatientAsync();
        await CreateAsync($"/api/v1/patients/{patientId}/invoices", new
        {
            lineItems = new[] { new { description = "Exam", quantity = 1, unitFee = 10m } }
        });
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await _admin.GetAsync($"/api/v1/patients/{patientId}/invoices?from={today}&to={today}");

        (await response.Content.ReadFromJsonAsync<Paged>())!.TotalCount.Should().Be(1);
    }
}
