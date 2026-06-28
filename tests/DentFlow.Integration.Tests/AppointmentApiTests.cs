using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Integration tests for appointment booking and status transitions.
/// These tests catch real bugs in the status state machine and business rules
/// that unit tests with mocked repos cannot detect.
/// </summary>
[Collection("Integration")]
public class AppointmentApiTests(DentFlowAppFactory factory)
{
    private readonly HttpClient _admin = factory.CreateAdminClient();
    private readonly HttpClient _receptionist = factory.CreateReceptionistClient();

    // ── POST /api/v1/appointments ─────────────────────────────────────────

    [Fact]
    public async Task BookAppointment_ValidRequest_Returns201WithScheduledStatus()
    {
        var (patientId, providerId, typeId) = await SeedBookingPrerequisites();

        var response = await _admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId,
            providerId,
            appointmentTypeId = typeId,
            startAt = DateTime.UtcNow.AddDays(3).ToString("o"),
            endAt = DateTime.UtcNow.AddDays(3).AddHours(1).ToString("o"),
            chiefComplaint = "Routine checkup",
            source = "Staff"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var appt = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        appt!.Status.Should().Be("Scheduled");
        appt.PatientId.Should().Be(patientId);
    }

    [Fact]
    public async Task BookAppointment_EndBeforeStart_Returns400()
    {
        var (patientId, providerId, typeId) = await SeedBookingPrerequisites();

        var response = await _admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId,
            providerId,
            appointmentTypeId = typeId,
            startAt = DateTime.UtcNow.AddDays(1).AddHours(2).ToString("o"),
            endAt = DateTime.UtcNow.AddDays(1).AddHours(1).ToString("o"), // end < start
            chiefComplaint = "Checkup",
            source = "Staff"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookAppointment_ProviderConflict_Returns409()
    {
        var (patientId, providerId, typeId) = await SeedBookingPrerequisites();
        var start = DateTime.UtcNow.AddDays(5);
        var end = start.AddHours(1);

        // First booking
        await _admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId,
            providerId,
            appointmentTypeId = typeId,
            startAt = start.ToString("o"),
            endAt = end.ToString("o"),
            chiefComplaint = "First",
            source = "Staff"
        });

        // Same provider, overlapping time
        var response = await _admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId = Guid.NewGuid(), // different patient
            providerId,
            appointmentTypeId = typeId,
            startAt = start.AddMinutes(30).ToString("o"),
            endAt = end.AddMinutes(30).ToString("o"),
            chiefComplaint = "Overlap",
            source = "Staff"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Status transitions ────────────────────────────────────────────────

    [Fact]
    public async Task StatusTransition_Scheduled_To_CheckedIn_Succeeds()
    {
        var apptId = await CreateBookedAppointment();

        var response = await _admin.PatchAsJsonAsync($"/api/v1/appointments/{apptId}/status",
            new { status = "CheckedIn" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var appt = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        appt!.Status.Should().Be("CheckedIn");
    }

    [Fact]
    public async Task StatusTransition_Scheduled_To_InProgress_Succeeds()
    {
        // Scheduled → InProgress is allowed (skipping CheckedIn)
        var apptId = await CreateBookedAppointment();

        var response = await _admin.PatchAsJsonAsync($"/api/v1/appointments/{apptId}/status",
            new { status = "InProgress" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var appt = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        appt!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task StatusTransition_Completed_To_Cancelled_Returns400()
    {
        // Cannot transition from a terminal state
        var apptId = await CreateBookedAppointment();
        await _admin.PatchAsJsonAsync($"/api/v1/appointments/{apptId}/status",
            new { status = "InProgress" });
        await _admin.PatchAsJsonAsync($"/api/v1/appointments/{apptId}/status",
            new { status = "Completed" });

        var response = await _admin.PatchAsJsonAsync($"/api/v1/appointments/{apptId}/status",
            new { status = "Cancelled" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StatusTransition_Scheduled_To_Completed_Returns400()
    {
        // Cannot jump from Scheduled directly to Completed — must go through InProgress
        var apptId = await CreateBookedAppointment();

        var response = await _admin.PatchAsJsonAsync($"/api/v1/appointments/{apptId}/status",
            new { status = "Completed" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelAppointment_ScheduledAppointment_Returns200()
    {
        var apptId = await CreateBookedAppointment();

        var response = await _admin.PostAsJsonAsync($"/api/v1/appointments/{apptId}/cancel",
            new { reason = "Patient request", cancelledBy = "Reception" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var appt = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        appt!.Status.Should().Be("Cancelled");
    }

    // ── GET /api/v1/appointments ──────────────────────────────────────────

    [Fact]
    public async Task ListAppointments_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var apptId = await CreateBookedAppointment();
        await _admin.PostAsJsonAsync($"/api/v1/appointments/{apptId}/cancel",
            new { reason = "Test", cancelledBy = "Test" });

        var response = await _admin.GetAsync("/api/v1/appointments?status=Cancelled");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedDto<AppointmentDto>>();
        body!.Items.Should().AllSatisfy(a => a.Status.Should().Be("Cancelled"));
    }

    // ── Admin override ────────────────────────────────────────────────────

    [Fact]
    public async Task AdminOverride_CanSetAnyStatus_BypassingTransitionRules()
    {
        var apptId = await CreateBookedAppointment();

        // Direct Scheduled → Completed via admin override (normally invalid)
        var response = await _admin.PostAsJsonAsync($"/api/v1/appointments/{apptId}/override-status",
            new { newStatus = "Completed", reason = "Admin correction" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var appt = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        appt!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task AdminOverride_Receptionist_Returns403()
    {
        var apptId = await CreateBookedAppointment();

        var response = await _receptionist.PostAsJsonAsync($"/api/v1/appointments/{apptId}/override-status",
            new { newStatus = "Completed", reason = "Should not work" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<(Guid patientId, Guid providerId, Guid typeId)> SeedBookingPrerequisites()
    {
        var patientResp = await _admin.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Test",
            lastName = "Patient",
            email = $"appt-test-{Guid.NewGuid():N}@test.com",
            smsOptIn = false,
            emailOptIn = false
        });
        var patient = await patientResp.Content.ReadFromJsonAsync<PatientDto>();

        var staffResp = await _admin.PostAsJsonAsync("/api/v1/staff", new
        {
            staffType = "Dentist",
            firstName = "Dr",
            lastName = "Test",
            email = $"dr-test-{Guid.NewGuid():N}@clinic.com",
            colorHex = "#3B82F6"
        });
        var staff = await staffResp.Content.ReadFromJsonAsync<StaffDto>();

        var typeResp = await _admin.PostAsJsonAsync("/api/v1/appointment-types", new
        {
            name = $"test-type-{Guid.NewGuid():N}",
            defaultDurationMinutes = 60,
            colorHex = "#10B981"
        });
        var type = await typeResp.Content.ReadFromJsonAsync<AppointmentTypeDto>();

        return (patient!.Id, staff!.Id, type!.Id);
    }

    private async Task<Guid> CreateBookedAppointment()
    {
        var (patientId, providerId, typeId) = await SeedBookingPrerequisites();

        var response = await _admin.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId,
            providerId,
            appointmentTypeId = typeId,
            startAt = DateTime.UtcNow.AddDays(7).ToString("o"),
            endAt = DateTime.UtcNow.AddDays(7).AddHours(1).ToString("o"),
            chiefComplaint = "Checkup",
            source = "Staff"
        });

        var appt = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        return appt!.Id;
    }
}

internal record AppointmentDto(Guid Id, Guid PatientId, string Status);
internal record StaffDto(Guid Id, string FirstName, string LastName);
internal record AppointmentTypeDto(Guid Id, string Name);
