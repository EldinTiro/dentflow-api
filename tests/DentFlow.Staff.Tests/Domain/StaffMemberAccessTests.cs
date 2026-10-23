using FluentAssertions;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Tests.Domain;

/// <summary>
/// People-merge plan 1.1 / 1.2: a person is a staff profile; signing in is an optional
/// account linked to it, and who can be booked is decided on the server.
/// </summary>
public class StaffMemberAccessTests
{
    private static StaffMember Profile(string? email = "ana@clinic.com", StaffType type = StaffType.Dentist) =>
        StaffMember.Create(type, "Ana", "Babić", email, null, null);

    [Fact]
    public void LinkUser_SetsUserId()
    {
        var staff = Profile();
        var userId = Guid.NewGuid();

        var result = staff.LinkUser(userId);

        result.IsError.Should().BeFalse();
        staff.UserId.Should().Be(userId);
    }

    [Fact]
    public void LinkUser_WhenAlreadyLinked_ReturnsStaffAlreadyHasAccess()
    {
        var staff = Profile();
        staff.LinkUser(Guid.NewGuid());

        staff.LinkUser(Guid.NewGuid()).FirstError.Code.Should().Be("Staff.AlreadyHasAccess");
    }

    [Fact]
    public void LinkUser_WhenInactive_ReturnsStaffInactive()
    {
        var staff = Profile();
        staff.Deactivate();

        staff.LinkUser(Guid.NewGuid()).FirstError.Code.Should().Be("Staff.Inactive");
        staff.UserId.Should().BeNull();
    }

    [Fact]
    public void LinkUser_WithoutEmail_ReturnsEmailRequired()
    {
        var staff = Profile(email: null);

        staff.LinkUser(Guid.NewGuid()).FirstError.Code.Should().Be("Staff.EmailRequiredForAccess");
        staff.UserId.Should().BeNull();
    }

    [Fact]
    public void Unlink_ClearsUserId()
    {
        var staff = Profile();
        staff.LinkUser(Guid.NewGuid());

        staff.Unlink();

        staff.UserId.Should().BeNull();
    }

    // ── Bookability (D3) ────────────────────────────────────────────────

    [Theory]
    [InlineData(StaffType.Dentist)]
    [InlineData(StaffType.Hygienist)]
    [InlineData(StaffType.DentalAssistant)]
    public void IsBookable_IsTrueForDentistAndClinicalNurseTypes(StaffType type) =>
        Profile(type: type).IsBookable.Should().BeTrue();

    [Theory]
    [InlineData(StaffType.Receptionist)]
    [InlineData(StaffType.ClinicAdmin)]
    [InlineData(StaffType.OfficeManager)]
    public void IsBookable_IsFalseForReceptionAndOffice(StaffType type) =>
        Profile(type: type).IsBookable.Should().BeFalse();

    [Fact]
    public void IsBookable_IsFalseWhenInactive()
    {
        var staff = Profile();
        staff.Deactivate();

        staff.IsBookable.Should().BeFalse();
    }
}
