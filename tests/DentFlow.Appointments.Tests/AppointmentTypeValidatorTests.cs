using FluentAssertions;
using DentFlow.Appointments.Application.Commands;

namespace DentFlow.Appointments.Tests;

/// <summary>QA pass 2026-10-31 (N-02): a service type with a default fee of -5 KM was saved.</summary>
public class AppointmentTypeValidatorTests
{
    private static CreateAppointmentTypeCommand Create(decimal? fee) => new("Filling", 45, null, null, false, fee);
    private static UpdateAppointmentTypeCommand Update(decimal? fee) => new(Guid.NewGuid(), "Filling", 45, null, null, false, fee);

    [Fact]
    public void ANegativeDefaultFee_IsRejected()
    {
        new CreateAppointmentTypeCommandValidator().Validate(Create(-5m)).IsValid.Should().BeFalse();
        new UpdateAppointmentTypeCommandValidator().Validate(Update(-5m)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(80.0)]
    public void NoFeeOrAPositiveFee_IsValid(double? fee) =>
        new CreateAppointmentTypeCommandValidator().Validate(Create((decimal?)fee)).IsValid.Should().BeTrue();
}
