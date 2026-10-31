using FluentAssertions;
using DentFlow.Appointments.Application.Commands;

namespace DentFlow.Appointments.Tests;

/// <summary>
/// QA pass 2026-10-31 (N-04): booking in the past was refused, but moving an appointment
/// into the past (PUT /appointments/{id}/reschedule to 1 October) returned 200.
/// Rescheduling picks a new start time, so the same rule applies.
/// </summary>
public class RescheduleAppointmentValidatorTests
{
    private readonly RescheduleAppointmentCommandValidator _sut = new();

    private static RescheduleAppointmentCommand ValidCommand(DateTime? newStartAt = null)
    {
        var start = newStartAt ?? DateTime.UtcNow.AddDays(1);
        return new RescheduleAppointmentCommand(Guid.NewGuid(), start, start.AddMinutes(30));
    }

    [Fact]
    public void MovingToAFutureTime_IsValid() =>
        _sut.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void MovingIntoThePast_IsRejected()
    {
        var result = _sut.Validate(ValidCommand(DateTime.UtcNow.AddDays(-30)));

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("An appointment cannot start in the past.");
    }

    [Fact]
    public void MovingToAFewMinutesAgo_IsAllowed_LikeBooking() =>
        _sut.Validate(ValidCommand(DateTime.UtcNow.AddMinutes(-3))).IsValid.Should().BeTrue();
}
