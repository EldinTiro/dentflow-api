using FluentAssertions;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Tests.Domain;

/// <summary>People-merge plan B6 / R1: staff are deactivated, not deleted.</summary>
public class StaffMemberStatusTests
{
    private static StaffMember Profile() => StaffMember.Create(StaffType.Dentist, "Ana", "Babić", "ana@clinic.com", null, null);

    [Fact]
    public void Deactivate_SetsInactive_AndTheTerminationDate()
    {
        var staff = Profile();

        staff.Deactivate().IsError.Should().BeFalse();

        staff.IsActive.Should().BeFalse();
        staff.TerminationDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        staff.IsBookable.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ReturnsConflict()
    {
        var staff = Profile();
        staff.Deactivate();

        staff.Deactivate().FirstError.Code.Should().Be(StaffErrors.AlreadyInactive.Code);
    }

    [Fact]
    public void Activate_ClearsTheTerminationDate()
    {
        var staff = Profile();
        staff.Deactivate();

        staff.Activate().IsError.Should().BeFalse();

        staff.IsActive.Should().BeTrue();
        staff.TerminationDate.Should().BeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ReturnsConflict()
    {
        Profile().Activate().FirstError.Code.Should().Be(StaffErrors.AlreadyActive.Code);
    }
}
