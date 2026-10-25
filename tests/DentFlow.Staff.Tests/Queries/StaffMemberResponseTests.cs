using FluentAssertions;
using DentFlow.Staff.Application;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Tests.Queries;

/// <summary>People-merge plan 1.2: the web reads bookability instead of hard-coding staff types.</summary>
public class StaffMemberResponseTests
{
    [Theory]
    [InlineData(StaffType.Dentist, true)]
    [InlineData(StaffType.Receptionist, false)]
    public void FromEntity_CarriesIsBookable(StaffType type, bool expected) =>
        StaffMemberResponse.FromEntity(StaffMember.Create(type, "A", "B", null, null, null))
            .IsBookable.Should().Be(expected);
}
