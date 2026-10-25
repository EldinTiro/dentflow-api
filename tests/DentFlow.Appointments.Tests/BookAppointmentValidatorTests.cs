using FluentAssertions;
using DentFlow.Appointments.Application.Commands;

namespace DentFlow.Appointments.Tests;

public class BookAppointmentValidatorTests
{
    private readonly BookAppointmentCommandValidator _sut = new();

    private static BookAppointmentCommand ValidCommand(DateTime? startAt = null, TimeSpan? length = null, string? notes = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        return new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, start + (length ?? TimeSpan.FromMinutes(30)),
            null, notes, null, false, "Staff");
    }

    // DT-08: a 24-hour visit and a booking in 2099 were accepted
    [Fact]
    public void AnAppointmentLongerThan8Hours_IsRejected() =>
        _sut.Validate(ValidCommand(length: TimeSpan.FromHours(24))).IsValid.Should().BeFalse();

    [Fact]
    public void AFullWorkingDay_IsAllowed() =>
        _sut.Validate(ValidCommand(length: TimeSpan.FromHours(8))).IsValid.Should().BeTrue();

    [Fact]
    public void ABookingMoreThanTwoYearsAhead_IsRejected() =>
        _sut.Validate(ValidCommand(DateTime.UtcNow.AddYears(73))).IsValid.Should().BeFalse();

    // DT-33: notes had no maximum
    [Fact]
    public void NotesOver2000Characters_AreRejected() =>
        _sut.Validate(ValidCommand(notes: new string('x', 2001))).IsValid.Should().BeFalse();

    [Fact]
    public void FutureBooking_IsValid() =>
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();

    // QA-018: a booking at 00:30 "today" while it was already 01:15 returned 201
    [Fact]
    public void BookingInThePast_IsRejected()
    {
        var result = _sut.Validate(ValidCommand(DateTime.UtcNow.AddMinutes(-45)));

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("An appointment cannot start in the past.");
    }

    [Fact]
    public void BookingAFewMinutesAgo_IsAllowed_ForClockSkewAndWalkIns() =>
        _sut.Validate(ValidCommand(DateTime.UtcNow.AddMinutes(-3))).IsValid.Should().BeTrue();
}
