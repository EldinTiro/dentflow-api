using FluentAssertions;
using DentFlow.Appointments.Application.Commands;

namespace DentFlow.Appointments.Tests;

/// <summary>
/// The shape of a reschedule. "Not into the past" (N-04) needs the visit's current start, so
/// the handler checks it; see RescheduleApiTests.
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
    public void AnEndBeforeTheStart_IsRejected()
    {
        var start = DateTime.UtcNow.AddDays(1);

        _sut.Validate(new RescheduleAppointmentCommand(Guid.NewGuid(), start, start.AddMinutes(-30)))
            .IsValid.Should().BeFalse();
    }
}
