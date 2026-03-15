using FluentAssertions;
using NSubstitute;
using PearlDesk.Staff.Application;
using PearlDesk.Staff.Application.Commands;
using PearlDesk.Staff.Application.Interfaces;
using PearlDesk.Staff.Domain;
namespace PearlDesk.Staff.Tests.Commands;
public class CreateStaffMemberCommandHandlerTests
{
    private readonly IStaffRepository _repo = Substitute.For<IStaffRepository>();
    private readonly CreateStaffMemberCommandHandler _sut;
    public CreateStaffMemberCommandHandlerTests()
    {
        _sut = new CreateStaffMemberCommandHandler(_repo);
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
