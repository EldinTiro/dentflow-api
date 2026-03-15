﻿using FluentAssertions;
using NSubstitute;
using PearlDesk.Staff.Application.Commands;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Application.Queries;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Tests.Queries;

public class GetStaffMemberByIdQueryHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly GetStaffMemberByIdQueryHandler _sut;

    public GetStaffMemberByIdQueryHandlerTests()
    {
        _sut = new GetStaffMemberByIdQueryHandler(_repo);
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
    private readonly DeleteStaffMemberCommandHandler _sut;

    public DeleteStaffMemberCommandHandlerTests()
    {
        _sut = new DeleteStaffMemberCommandHandler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingStaff_SoftDeletes()
    {
        // Arrange
        var staff = StaffMember.Create(StaffType.Receptionist, "Jane", "Doe", null, null, null);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);

        // Act
        var result = await _sut.Handle(new DeleteStaffMemberCommand(staff.Id), CancellationToken.None);

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
        var result = await _sut.Handle(new DeleteStaffMemberCommand(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Staff.NotFound");
        await _repo.DidNotReceive().SoftDeleteAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
    }
}

