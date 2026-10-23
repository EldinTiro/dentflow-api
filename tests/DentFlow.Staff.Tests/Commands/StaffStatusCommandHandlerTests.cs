using ErrorOr;
using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Staff.Application.Commands;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;
using DentFlow.Staff.Tests.Support;

namespace DentFlow.Staff.Tests.Commands;

/// <summary>People-merge plan B6: deactivate / reactivate / delete a staff profile.</summary>
public class StaffStatusCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserAccountLifecycle _accounts = Substitute.For<IUserAccountLifecycle>();
    private readonly IAppointmentReader _appointments = Substitute.For<IAppointmentReader>();
    private readonly PassThroughTransactionRunner _transaction = new();
    private static readonly AccountActor Owner = new(Guid.NewGuid(), [Roles.ClinicOwner]);

    private StaffMember Existing(Guid? userId = null)
    {
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Babić", "ana@clinic.com", null, null);
        if (userId is { } id) staff.LinkUser(id);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        return staff;
    }

    private DeactivateStaffMemberCommandHandler Deactivator() => new(_repo, _accounts, _appointments, _transaction);

    [Fact]
    public async Task Deactivate_LinkedProfile_DeactivatesProfileAndAccount()
    {
        var userId = Guid.NewGuid();
        var staff = Existing(userId);
        _accounts.DeactivateAsync(userId, Owner, Arg.Any<CancellationToken>()).Returns(Result.Success);

        var result = await Deactivator().Handle(new DeactivateStaffMemberCommand(staff.Id, Owner), default);

        result.IsError.Should().BeFalse();
        result.Value.IsActive.Should().BeFalse();
        result.Value.IsBookable.Should().BeFalse();
        await _accounts.Received(1).DeactivateAsync(userId, Owner, Arg.Any<CancellationToken>());
        await _repo.Received(1).UpdateAsync(staff, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_WithUpcomingAppointments_Returns409WithTheCount_AndChangesNothing()
    {
        var staff = Existing(Guid.NewGuid());
        _appointments.CountUpcomingForProviderAsync(staff.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(3);

        var result = await Deactivator().Handle(new DeactivateStaffMemberCommand(staff.Id, Owner), default);

        result.FirstError.Code.Should().Be("Staff.HasUpcomingAppointments");
        result.FirstError.Metadata.Should().ContainKey("count").WhoseValue.Should().Be(3);
        staff.IsActive.Should().BeTrue();
        await _accounts.DidNotReceiveWithAnyArgs().DeactivateAsync(default, default!, default);
    }

    [Fact]
    public async Task Deactivate_WhenTheAccountGuardRefuses_ReturnsItsError_AndTheProfileStaysActive()
    {
        var userId = Guid.NewGuid();
        var staff = Existing(userId);
        _accounts.DeactivateAsync(userId, Owner, Arg.Any<CancellationToken>())
            .Returns(Error.Conflict("User.LastClinicOwner", "last owner"));

        var result = await Deactivator().Handle(new DeactivateStaffMemberCommand(staff.Id, Owner), default);

        result.FirstError.Code.Should().Be("User.LastClinicOwner");
        staff.IsActive.Should().BeTrue();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_AlreadyInactive_ReturnsConflict()
    {
        var staff = Existing();
        staff.Deactivate();

        var result = await Deactivator().Handle(new DeactivateStaffMemberCommand(staff.Id, Owner), default);

        result.FirstError.Code.Should().Be(StaffErrors.AlreadyInactive.Code);
    }

    [Fact]
    public async Task Reactivate_ReactivatesTheProfile_ButNotTheAccount()
    {
        var staff = Existing(Guid.NewGuid());
        staff.Deactivate();

        var result = await new ReactivateStaffMemberCommandHandler(_repo).Handle(new ReactivateStaffMemberCommand(staff.Id), default);

        result.Value.IsActive.Should().BeTrue();
        await _repo.Received(1).UpdateAsync(staff, Arg.Any<CancellationToken>());
        await _accounts.DidNotReceiveWithAnyArgs().DeactivateAsync(default, default!, default);
    }

    [Fact]
    public async Task Delete_ProfileWithAppointments_ReturnsHasHistory_AndDeletesNothing()
    {
        var staff = Existing();
        _appointments.ProviderHasAnyAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeleteStaffMemberCommandHandler(_repo, _accounts, _appointments, _transaction)
            .Handle(new DeleteStaffMemberCommand(staff.Id, Owner), default);

        result.FirstError.Code.Should().Be(StaffErrors.HasHistory.Code);
        await _repo.DidNotReceive().SoftDeleteAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
    }
}
