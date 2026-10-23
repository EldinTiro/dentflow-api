using FluentAssertions;
using NSubstitute;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Commands;
using DentFlow.Staff.Application.Interfaces;
using DentFlow.Staff.Domain;
using DentFlow.Staff.Tests.Support;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using ErrorOr;
namespace DentFlow.Staff.Tests.Commands;
public class CreateStaffMemberCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly IUserProvisioningService _provisioning = Substitute.For<IUserProvisioningService>();
    private readonly PassThroughTransactionRunner _transaction = new();
    private readonly CreateStaffMemberCommandHandler _sut;
    public CreateStaffMemberCommandHandlerTests()
    {
        _sut = new CreateStaffMemberCommandHandler(_repo, _provisioning, _transaction);
    }

    private static CreateStaffMemberCommand WithAccess(string? email = "amra@clinic.com", string role = Roles.Dentist) =>
        new(StaffType.Dentist, "Amra", "Begić", email, null, null, null, null, new StaffAccessRequest(role));

    // ── People-merge plan 2.3: profile + account in one call ──

    [Fact]
    public async Task Handle_WithAccess_CreatesProfileAndAccount_AndReturnsTheTempPasswordOnce()
    {
        var userId = Guid.NewGuid();
        _provisioning.CreateUserAsync(default!, default!, default!, default!, default!, default, default).ReturnsForAnyArgs(userId);

        var result = await _sut.Handle(WithAccess(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.TempPassword.Should().NotBeNullOrWhiteSpace();
        await _repo.Received(1).AddAsync(Arg.Is<StaffMember>(s => s.UserId == userId), Arg.Any<CancellationToken>());
        await _provisioning.Received(1).CreateUserAsync("amra@clinic.com", result.Value.TempPassword!, "Amra", "Begić", Roles.Dentist, Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAccessButNoEmail_ReturnsValidation_AndCreatesNothing()
    {
        var result = await _sut.Handle(WithAccess(email: null), CancellationToken.None);

        result.FirstError.Code.Should().Be("Staff.EmailRequiredForAccess");
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithAccessAndAnUnknownRole_ReturnsValidation_AndCreatesNothing()
    {
        var result = await _sut.Handle(WithAccess(role: "Admin"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Staff.InvalidAccessRole");
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithAccess_WhenTheAccountCannotBeCreated_FailsInsideOneTransaction()
    {
        _provisioning.CreateUserAsync(default!, default!, default!, default!, default!, default, default)
            .ReturnsForAnyArgs(Error.Conflict("User.EmailAlreadyExists", "taken"));

        var result = await _sut.Handle(WithAccess(), CancellationToken.None);

        result.FirstError.Code.Should().Be("User.EmailAlreadyExists");
        _transaction.Calls.Should().BeGreaterThan(0, "the profile insert must be rolled back with the failed account");
    }

    [Fact]
    public async Task Handle_WithoutAccess_ReturnsNoTempPassword()
    {
        var result = await _sut.Handle(WithAccess() with { Access = null }, CancellationToken.None);

        result.Value.TempPassword.Should().BeNull();
        await _provisioning.DidNotReceiveWithAnyArgs().CreateUserAsync(default!, default!, default!, default!, default!, default, default);
    }
    [Fact]
    public async Task Handle_ValidCommand_CreatesAndReturnsStaffMember()
    {
        // Arrange
        var command = new CreateStaffMemberCommand(
            StaffType: StaffType.Dentist,
            FirstName: "Alice",
            LastName: "Smith",
            Email: "alice@clinic.com",
            Phone: "+1234567890",
            HireDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Specialty: "General",
            ColorHex: "#3B82F6");
        _repo.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
             .Returns((StaffMember?)null);
        // Act
        var result = await _sut.Handle(command, CancellationToken.None);
        // Assert
        result.IsError.Should().BeFalse();
        result.Value.FirstName.Should().Be("Alice");
        result.Value.LastName.Should().Be("Smith");
        result.Value.StaffType.Should().Be(StaffType.Dentist);
        await _repo.Received(1).AddAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictError()
    {
        // Arrange
        var existing = StaffMember.Create(StaffType.Dentist, "Existing", "Staff", "alice@clinic.com", null, null);
        var command = new CreateStaffMemberCommand(
            StaffType: StaffType.Dentist,
            FirstName: "Alice",
            LastName: "Duplicate",
            Email: "alice@clinic.com",
            Phone: null,
            HireDate: null,
            Specialty: null,
            ColorHex: null);
        _repo.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
             .Returns(existing);
        // Act
        var result = await _sut.Handle(command, CancellationToken.None);
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Staff.AlreadyExists");
        await _repo.DidNotReceive().AddAsync(Arg.Any<StaffMember>(), Arg.Any<CancellationToken>());
    }
}
