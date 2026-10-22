using FluentAssertions;
using DentFlow.Appointments.Application.Commands;

namespace DentFlow.Appointments.Tests;

public class BookAppointmentValidatorTests
{
    private readonly BookAppointmentCommandValidator _sut = new();

    private static BookAppointmentCommand ValidCommand(DateTime? startAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        return new BookAppointmentCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, start.AddMinutes(30),
            null, null, null, false, "Staff");
    }

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
