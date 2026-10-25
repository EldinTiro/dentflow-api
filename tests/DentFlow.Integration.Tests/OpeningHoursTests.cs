using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-13: Monday 08:00 → 07:00 was saved ("Opening hours saved"), leaving Monday with no
/// slots; and the response carried a legacy 08:00–22:30 window next to the weekly schedule.
/// </summary>
[Collection("Integration")]
public class OpeningHoursTests(DentFlowAppFactory factory)
{
    private static string Schedule(string mondayStart, string mondayEnd, bool mondayOpen = true) => JsonSerializer.Serialize(new
    {
        monday = new { isOpen = mondayOpen, start = mondayStart, end = mondayEnd },
        tuesday = new { isOpen = true, start = "08:00", end = "17:00" },
        sunday = new { isOpen = false, start = "08:00", end = "12:00" },
    });

    private HttpClient Owner() => factory.CreateAuthenticatedClient([Roles.ClinicOwner], "hours-clinic");

    [Fact]
    public async Task AnOpenDayEndingBeforeItStarts_IsRejected()
    {
        var response = await Owner().PutAsJsonAsync("/api/v1/tenant/settings",
            new { slotDurationMinutes = 30, weeklyScheduleJson = Schedule("08:00", "07:00") });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Clinic.InvalidOpeningHours");
    }

    [Theory]
    [InlineData("8 am", "17:00")]
    [InlineData("08:00", "25:00")]
    public async Task TimesThatAreNotHoursAndMinutes_AreRejected(string start, string end)
    {
        var response = await Owner().PutAsJsonAsync("/api/v1/tenant/settings",
            new { slotDurationMinutes = 30, weeklyScheduleJson = Schedule(start, end) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AClosedDay_DoesNotNeedValidHours_AndTheScheduleIsTheOnlySource()
    {
        var owner = Owner();

        (await owner.PutAsJsonAsync("/api/v1/tenant/settings",
            new { slotDurationMinutes = 30, weeklyScheduleJson = Schedule("08:00", "07:00", mondayOpen: false) }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await owner.GetFromJsonAsync<JsonElement>("/api/v1/tenant/settings");
        settings.TryGetProperty("workdayStart", out _).Should().BeFalse();
        settings.TryGetProperty("workdayEnd", out _).Should().BeFalse();
        settings.GetProperty("weeklyScheduleJson").GetString().Should().Contain("monday");
    }
}
