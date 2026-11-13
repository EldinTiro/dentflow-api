using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Staff.Application.Commands;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;
using DentFlow.Staff.Tests.Support;

namespace DentFlow.Staff.Tests.Commands;

/// <summary>User request 2026-11-13: the Administrator deletes a staff profile for good.</summary>
public class PurgeStaffMemberCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IAppointmentReader _appointments = Substitute.For<IAppointmentReader>();
    private readonly IPeoplePurger _purger = Substitute.For<IPeoplePurger>();
    private readonly PassThroughTransactionRunner _transaction = new();

    private PurgeStaffMemberCommandHandler Sut() => new(_repo, _appointments, _purger, _transaction);

    private StaffMember Existing(Guid? userId = null)
    {
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Babić", "ana@clinic.com", null, null);
        if (userId is { } id) staff.LinkUser(id);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        return staff;
    }

    [Fact]
    public async Task Handle_OwnProfile_ReturnsCannotDeleteSelf()
    {
        var me = Guid.NewGuid();
        var staff = Existing(me);

        var result = await Sut().Handle(new PurgeStaffMemberCommand(staff.Id, new AccountActor(me, [Roles.ClinicOwner])), default);

        result.FirstError.Code.Should().Be("Staff.CannotDeleteSelf");
        await _purger.DidNotReceiveWithAnyArgs().PurgeStaffMemberAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_ProfileWithoutHistory_PurgesItWithItsAccount()
    {
        var account = Guid.NewGuid();
        var staff = Existing(account);

        var result = await Sut().Handle(new PurgeStaffMemberCommand(staff.Id, new AccountActor(Guid.NewGuid(), [Roles.ClinicOwner])), default);

        result.IsError.Should().BeFalse();
        await _purger.Received(1).PurgeStaffMemberAsync(staff.Id, account, Arg.Any<CancellationToken>());
    }
}
