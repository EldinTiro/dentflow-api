using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// PUT /appointments/{id}/reschedule with an optional newProviderId: the day view lets a visit be
/// dragged to another time and to another doctor's column.
/// </summary>
[Collection("Integration")]
public class RescheduleApiTests(DentFlowAppFactory factory)
{
    private readonly HttpClient _admin = factory.CreateAdminClient();

    private record IdDto(Guid Id);
    private record MovedDto(Guid Id, Guid ProviderId, DateTime StartAt, DateTime EndAt, string Status);

    private async Task<Guid> StaffAsync(string staffType = "Dentist")
    {
        var r = await _admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType, firstName = "Dr", lastName = "Move", email = $"move-{Guid.NewGuid():N}@clinic.com", colorHex = "#3B82F6"
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private async Task<Guid> PatientAsync()
    {
        var r = await _admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Move", lastName = "Patient", email = $"move-{Guid.NewGuid():N}@test.com", smsOptIn = false, emailOptIn = false
        });
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private async Task<Guid> TypeAsync()
    {
        var r = await _admin.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"move-{Guid.NewGuid():N}", defaultDurationMinutes = 60, colorHex = "#10B981"
        });
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private async Task<Guid> BookAsync(Guid providerId, DateTime start, Guid? patientId = null)
    {
        var r = await _admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId = patientId ?? await PatientAsync(),
            providerId,
            appointmentTypeId = await TypeAsync(),
            startAt = start.ToString("o"),
            endAt = start.AddHours(1).ToString("o"),
            chiefComplaint = "Checkup",
            source = "Staff"
        });
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static DateTime Day(int daysAhead, int hour) => DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour);

    private Task<HttpResponseMessage> MoveAsync(HttpClient client, Guid id, DateTime start, Guid? newProviderId = null) =>
        client.PutAsJsonAsync($"/api/v1/appointments/{id}/reschedule", new
        {
            newStartAt = start.ToString("o"),
            newEndAt = start.AddHours(1).ToString("o"),
            newProviderId,
        });

    [Fact]
    public async Task Reschedule_ToAnotherDoctor_MovesTheVisitToThem()
    {
        var eldina = await StaffAsync();
        var ana = await StaffAsync();
        var id = await BookAsync(eldina, Day(20, 9));

        var r = await MoveAsync(_admin, id, Day(20, 11), ana);

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        var moved = (await r.Content.ReadFromJsonAsync<MovedDto>())!;
        moved.ProviderId.Should().Be(ana);
        moved.StartAt.Should().Be(Day(20, 11));
    }

    [Fact]
    public async Task Reschedule_WithoutANewDoctor_KeepsTheDoctor()
    {
        var eldina = await StaffAsync();
        var id = await BookAsync(eldina, Day(21, 9));

        var r = await MoveAsync(_admin, id, Day(21, 13));

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<MovedDto>())!.ProviderId.Should().Be(eldina);
    }

    // The conflict check must look at the doctor the visit moves to, not the old one
    [Fact]
    public async Task Reschedule_ToADoctorWhoIsBusyThen_Returns409()
    {
        var eldina = await StaffAsync();
        var ana = await StaffAsync();
        var id = await BookAsync(eldina, Day(22, 9));
        await BookAsync(ana, Day(22, 11));

        var r = await MoveAsync(_admin, id, Day(22, 11), ana);

        r.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Appointment.ProviderConflict");
    }

    [Fact]
    public async Task Reschedule_ToADoctorOnLeave_Returns409()
    {
        var eldina = await StaffAsync();
        var ana = await StaffAsync();
        var id = await BookAsync(eldina, Day(23, 9));
        (await _admin.PostAsJsonAsync($"/api/v1/staff/{ana}/blocked-times", new
        {
            startAt = Day(23, 0).ToString("o"), endAt = Day(23, 23).ToString("o"), absenceType = "Vacation"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var r = await MoveAsync(_admin, id, Day(23, 11), ana);

        r.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Appointment.ProviderUnavailable");
    }

    [Fact]
    public async Task Reschedule_ToStaffWhoTakesNoAppointments_Returns409()
    {
        var eldina = await StaffAsync();
        var receptionist = await StaffAsync("Receptionist");
        var id = await BookAsync(eldina, Day(24, 9));

        var r = await MoveAsync(_admin, id, Day(24, 9), receptionist);

        r.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await r.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Appointment.ProviderNotBookable");
    }

    [Fact]
    public async Task Reschedule_ToADoctorOfAnotherClinic_Returns404()
    {
        var eldina = await StaffAsync();
        var id = await BookAsync(eldina, Day(25, 9));

        var r = await MoveAsync(_admin, id, Day(25, 11), Guid.NewGuid());

        r.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Bug: a dentist could book and was offered "Reschedule" in the app, but the API answered 403
    [Fact]
    public async Task Reschedule_AsADentist_IsAllowed()
    {
        var eldina = await StaffAsync();
        var id = await BookAsync(eldina, Day(26, 9));

        var r = await MoveAsync(factory.CreateDentistClient(), id, Day(26, 12));

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
    }
}
