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

/// <summary>People-merge plan 2.2: give an existing staff profile a sign-in account.</summary>
public class GrantStaffAccessCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserProvisioningService _provisioning = Substitute.For<IUserProvisioningService>();
    private readonly PassThroughTransactionRunner _transaction = new();
    private readonly GrantStaffAccessCommandHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public GrantStaffAccessCommandHandlerTests() =>
        _sut = new GrantStaffAccessCommandHandler(_repo, _provisioning, _transaction);

    private StaffMember Existing(string? email = "ana@clinic.com")
    {
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Babić", email, null, null);
        staff.SetTenant(TenantId);
        _repo.GetByIdAsync(staff.Id, Arg.Any<CancellationToken>()).Returns(staff);
        return staff;
    }

    private static GrantStaffAccessCommand Command(Guid staffId, string role = Roles.Dentist) => new(staffId, role);

    private void ProvisioningReturns(ErrorOr<Guid> result) =>
        _provisioning.CreateUserAsync(default!, default!, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(result);

    [Fact]
    public async Task Handle_ValidRequest_CreatesTheAccountAndLinksIt()
    {
        var staff = Existing();
        var userId = Guid.NewGuid();
        ProvisioningReturns(userId);

        var result = await _sut.Handle(Command(staff.Id), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.UserId.Should().Be(userId);
        result.Value.TempPassword.Should().NotBeNullOrWhiteSpace();
        staff.UserId.Should().Be(userId);
        await _provisioning.Received(1).CreateUserAsync(
            "ana@clinic.com", result.Value.TempPassword, "Ana", "Babić", Roles.Dentist, TenantId, Arg.Any<CancellationToken>());
        await _repo.Received(1).UpdateAsync(staff, Arg.Any<CancellationToken>());
        _transaction.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnknownProfile_ReturnsNotFound()
    {
        var result = await _sut.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("Staff.NotFound");
    }

    [Fact]
    public async Task Handle_AlreadyLinked_ReturnsConflict_AndCreatesNoAccount()
    {
        var staff = Existing();
        staff.LinkUser(Guid.NewGuid());

        var result = await _sut.Handle(Command(staff.Id), CancellationToken.None);

        result.FirstError.Code.Should().Be("Staff.AlreadyHasAccess");
        await _provisioning.DidNotReceiveWithAnyArgs().CreateUserAsync(default!, default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_InactiveProfile_ReturnsConflict()
    {
        var staff = Existing();
        staff.Deactivate();

        (await _sut.Handle(Command(staff.Id), CancellationToken.None)).FirstError.Code.Should().Be("Staff.Inactive");
    }

    [Fact]
    public async Task Handle_ProfileWithoutEmail_ReturnsValidation()
    {
        var staff = Existing(email: null);

        (await _sut.Handle(Command(staff.Id), CancellationToken.None)).FirstError.Code.Should().Be("Staff.EmailRequiredForAccess");
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsedOnThePlatform_ReturnsConflict_AndDoesNotLink()
    {
        var staff = Existing();
        ProvisioningReturns(Error.Conflict("User.EmailAlreadyExists", "taken"));

        var result = await _sut.Handle(Command(staff.Id), CancellationToken.None);

        result.FirstError.Code.Should().Be("User.EmailAlreadyExists");
        staff.UserId.Should().BeNull();
        await _repo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Theory]
    [InlineData(Roles.SuperAdmin)]
    [InlineData("Hygienist")]
    [InlineData("Admin")]
    public async Task Handle_RoleThatIsNotAClinicRole_ReturnsValidation(string role)
    {
        var staff = Existing();

        var result = await _sut.Handle(Command(staff.Id, role), CancellationToken.None);

        result.FirstError.Code.Should().Be("Staff.InvalidAccessRole");
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }
}
