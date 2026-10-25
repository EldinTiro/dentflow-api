using ErrorOr;
using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Staff.Application.Commands;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;
using DentFlow.Staff.Tests.Support;

namespace DentFlow.Staff.Tests.Commands;

/// <summary>People-merge plan 2.7: profile and account never show different names or emails.</summary>
public class UpdateStaffMemberCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserAccountLifecycle _accounts = Substitute.For<IUserAccountLifecycle>();
    private readonly UpdateStaffMemberCommandHandler _sut;

    public UpdateStaffMemberCommandHandlerTests()
    {
        _sut = new UpdateStaffMemberCommandHandler(_repo, _accounts, new PassThroughTransactionRunner());
        _accounts.RenameAsync(default, default!, default!, default).ReturnsForAnyArgs(Result.Success);
    }

    private StaffMember Profile(Guid? userId)
    {
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Babić", "ana@clinic.com", null, null);
        if (userId is { } id) staff.LinkUser(id);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        return staff;
    }

    private static UpdateStaffMemberCommand Update(Guid id, string first = "Ana", string last = "Babić", string? email = "ana@clinic.com") =>
        new(id, first, last, email, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Handle_LinkedProfile_RenamesTheAccountToo()
    {
        var userId = Guid.NewGuid();
        var staff = Profile(userId);

        var result = await _sut.Handle(Update(staff.Id, "Ana", "Hadžić"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _accounts.Received(1).RenameAsync(userId, "Ana", "Hadžić", Arg.Any<CancellationToken>());
    }

    // D1: while linked, the email is the login email and can't be changed here.
    [Fact]
    public async Task Handle_LinkedProfile_EmailChange_IsRefused_AndNothingIsSaved()
    {
        var staff = Profile(Guid.NewGuid());

        var result = await _sut.Handle(Update(staff.Id, email: "new@clinic.com"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Staff.EmailLockedByAccess");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        await _repo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_LinkedProfile_SameEmailInOtherCase_IsNotAChange()
    {
        var staff = Profile(Guid.NewGuid());

        (await _sut.Handle(Update(staff.Id, email: "ANA@clinic.com"), CancellationToken.None)).IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnlinkedProfile_CanChangeItsEmail_AndTouchesNoAccount()
    {
        var staff = Profile(null);

        var result = await _sut.Handle(Update(staff.Id, email: "new@clinic.com"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _accounts.DidNotReceiveWithAnyArgs().RenameAsync(default, default!, default!, default);
    }
}
