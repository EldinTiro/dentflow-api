using ErrorOr;
﻿using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Commands;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Application.Queries;
using DentFlow.Staff.Domain;

namespace DentFlow.Staff.Tests.Queries;

public class GetStaffMemberByIdQueryHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserAccountDirectory _accounts = Substitute.For<IUserAccountDirectory>();
    private readonly GetStaffMemberByIdQueryHandler _sut;

    public GetStaffMemberByIdQueryHandlerTests()
    {
        _sut = new GetStaffMemberByIdQueryHandler(_repo, _accounts);
        _accounts.GetSummariesAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, StaffAccessSummary>());
    }

    // People-merge plan 2.4
    [Fact]
    public async Task Handle_LinkedProfile_CarriesItsAccessSummary()
    {
        var staff = StaffMember.Create(StaffType.Dentist, "John", "Doe", "j@d.com", null, null);
        var userId = Guid.NewGuid();
        staff.LinkUser(userId);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        var summary = new StaffAccessSummary(userId, "Dentist", true, true, null);
        _accounts.GetSummariesAsync(default!, default).ReturnsForAnyArgs(new Dictionary<Guid, StaffAccessSummary> { [userId] = summary });

        var result = await _sut.Handle(new GetStaffMemberByIdQuery(staff.Id), CancellationToken.None);

        result.Value.Access.Should().Be(summary);
    }

    [Fact]
    public async Task Handle_UnlinkedProfile_HasNoAccess()
    {
        var staff = StaffMember.Create(StaffType.Receptionist, "Jane", "Doe", null, null, null);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        var result = await _sut.Handle(new GetStaffMemberByIdQuery(staff.Id), CancellationToken.None);

        result.Value.Access.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsStaffMember()
    {
        // Arrange
        var staff = StaffMember.Create(StaffType.Dentist, "John", "Doe", "j@d.com", null, null);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        // Act
        var result = await _sut.Handle(new GetStaffMemberByIdQuery(staff.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(staff.Id);
        result.Value.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_NonExistingId_ReturnsNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);

        // Act
        var result = await _sut.Handle(new GetStaffMemberByIdQuery(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Staff.NotFound");
    }
}

public class DeleteStaffMemberCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserAccountLifecycle _accounts = Substitute.For<IUserAccountLifecycle>();
    private readonly DentFlow.Staff.Tests.Support.PassThroughTransactionRunner _transaction = new();
    private readonly DeleteStaffMemberCommandHandler _sut;
    private static readonly AccountActor Owner = new(Guid.NewGuid(), ["ClinicOwner"]);

    public DeleteStaffMemberCommandHandlerTests()
    {
        _sut = new DeleteStaffMemberCommandHandler(_repo, _accounts, _transaction);
        _accounts.DeactivateAsync(default, default!, default).ReturnsForAnyArgs(Result.Success);
    }

    private static DeleteStaffMemberCommand Delete(Guid id) => new(id, Owner);

    private StaffMember LinkedProfile(Guid userId)
    {
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Linked", "ana@c.com", null, null);
        staff.LinkUser(userId);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        return staff;
    }

    // ── People-merge plan 2.6 (D4): the account follows the profile ──

    [Fact]
    public async Task Handle_LinkedProfile_DeactivatesTheAccount_InTheSameTransaction()
    {
        var userId = Guid.NewGuid();
        var staff = LinkedProfile(userId);

        var result = await _sut.Handle(Delete(staff.Id), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _accounts.Received(1).DeactivateAsync(userId, Owner, Arg.Any<CancellationToken>());
        await _repo.Received(1).SoftDeleteAsync(staff, Arg.Any<CancellationToken>());
        _transaction.Calls.Should().Be(1);
    }

    [Theory]
    [InlineData("User.Self")]
    [InlineData("User.LastClinicOwner")]
    public async Task Handle_WhenTheAccountGuardRefuses_ReturnsItsError_AndDeletesNothing(string code)
    {
        var staff = LinkedProfile(Guid.NewGuid());
        _accounts.DeactivateAsync(default, default!, default).ReturnsForAnyArgs(Error.Conflict(code, "no"));

        var result = await _sut.Handle(Delete(staff.Id), CancellationToken.None);

        result.FirstError.Code.Should().Be(code);
        await _repo.DidNotReceiveWithAnyArgs().SoftDeleteAsync(default!, default);
    }

    [Fact]
    public async Task Handle_UnlinkedProfile_LeavesAccountsAlone()
    {
        var staff = StaffMember.Create(StaffType.Receptionist, "Jane", "Doe", null, null, null);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        await _sut.Handle(Delete(staff.Id), CancellationToken.None);

        await _accounts.DidNotReceiveWithAnyArgs().DeactivateAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_ExistingStaff_SoftDeletes()
    {
        // Arrange
        var staff = StaffMember.Create(StaffType.Receptionist, "Jane", "Doe", null, null, null);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        // Act
        var result = await _sut.Handle(Delete(staff.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _repo.Received(1).SoftDeleteAsync(staff, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistingStaff_ReturnsNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((StaffMember?)null);

        // Act
        var result = await _sut.Handle(Delete(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Staff.NotFound");
        await _repo.DidNotReceive().SoftDeleteAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
    }
}

public class ListStaffMembersQueryHandlerAccessTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserAccountDirectory _accounts = Substitute.For<IUserAccountDirectory>();

    // People-merge plan 2.4: one account lookup per page, never one per row.
    [Fact]
    public async Task Handle_LooksUpAllLinkedAccountsInOneCall()
    {
        var staff = Enumerable.Range(0, 20).Select(i =>
        {
            var s = StaffMember.Create(StaffType.Dentist, $"Dr{i}", "X", $"d{i}@c.com", null, null);
            if (i % 2 == 0) s.LinkUser(Guid.NewGuid());
            return s;
        }).ToList();
        _repo.ListAsync(default, default, default, default, default, default).ReturnsForAnyArgs((staff, staff.Count));
        _accounts.GetSummariesAsync(default!, default).ReturnsForAnyArgs(
            staff.Where(s => s.UserId is not null)
                .ToDictionary(s => s.UserId!.Value, s => new StaffAccessSummary(s.UserId!.Value, "Dentist", true, false, null)));
        var sut = new ListStaffMembersQueryHandler(_repo, _accounts);

        var result = await sut.Handle(new ListStaffMembersQuery(null, null, 1, 20, null), CancellationToken.None);

        await _accounts.ReceivedWithAnyArgs(1).GetSummariesAsync(default!, default);
        await _accounts.Received(1).GetSummariesAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 10), Arg.Any<CancellationToken>());
        result.Value.Items.Count(i => i.Access is not null).Should().Be(10);
    }
}
