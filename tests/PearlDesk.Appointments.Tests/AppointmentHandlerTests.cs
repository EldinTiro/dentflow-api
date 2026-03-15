using FluentAssertions;
using NSubstitute;
using PearlDesk.Appointments.Application;
using PearlDesk.Appointments.Application.Commands;
using PearlDesk.Appointments.Application.Interfaces;
using PearlDesk.Appointments.Application.Queries;
using PearlDesk.Appointments.Domain;

namespace PearlDesk.Appointments.Tests;

public class BookAppointmentCommandHandlerTests
{
    private readonly IAppointmentRepository _repo = Substitute.For<IAppointmentRepository>();
    private readonly IAppointmentTypeRepository _typeRepo = Substitute.For<IAppointmentTypeRepository>();
    private readonly BookAppointmentCommandHandler _sut;

    public BookAppointmentCommandHandlerTests()
    {
        _sut = new BookAppointmentCommandHandler(_repo, _typeRepo);
    }

    private static BookAppointmentCommand ValidCommand(DateTime? startAt = null, DateTime? endAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddHours(1);
        var end = endAt ?? DateTime.UtcNow.AddHours(2);
        return new BookAppointmentCommand(
            PatientId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            AppointmentTypeId: Guid.NewGuid(),
            StartAt: start,
            EndAt: end,
            ChiefComplaint: "Checkup",
            Notes: null,
            OperatoryId: null,
            IsNewPatient: false,
            Source: "Staff");
    }

    [Fact]
    public async Task Handle_ValidCommand_BooksAppointment()
    {
        // Arrange
        var command = ValidCommand();
        var appointmentType = AppointmentType.Create("Checkup", 60);
        _typeRepo.GetByIdAsync(command.AppointmentTypeId, Arg.Any<CancellationToken>())
                 .Returns(appointmentType);
        _repo.HasProviderConflictAsync(
            command.ProviderId, command.StartAt, command.EndAt,
            null, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PatientId.Should().Be(command.PatientId);
        result.Value.Status.Should().Be(AppointmentStatus.Scheduled);

        await _repo.Received(1).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EndBeforeStart_ReturnsValidationError()
    {
        // Arrange
        var start = DateTime.UtcNow.AddHours(2);
        var end = DateTime.UtcNow.AddHours(1); // end before start
        var command = ValidCommand(startAt: start, endAt: end);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Appointment.InvalidTimeRange");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppointmentTypeNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = ValidCommand();
        _typeRepo.GetByIdAsync(command.AppointmentTypeId, Arg.Any<CancellationToken>())
                 .Returns((AppointmentType?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("AppointmentType.NotFound");
    }

    [Fact]
    public async Task Handle_ProviderConflict_ReturnsConflictError()
    {
        // Arrange
        var command = ValidCommand();
        var appointmentType = AppointmentType.Create("Checkup", 60);
        _typeRepo.GetByIdAsync(command.AppointmentTypeId, Arg.Any<CancellationToken>())
                 .Returns(appointmentType);
        _repo.HasProviderConflictAsync(
            command.ProviderId, command.StartAt, command.EndAt,
            null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Appointment.ProviderConflict");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}

public class AppointmentStatusCommandHandlerTests
{
    private readonly IAppointmentRepository _repo = Substitute.For<IAppointmentRepository>();

    [Fact]
    public async Task CancelAppointment_CompletedAppointment_ReturnsError()
    {
        // Arrange
        var sut = new CancelAppointmentCommandHandler(_repo);
        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));
        appointment.Complete();
        _repo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        // Act
        var result = await sut.Handle(
            new CancelAppointmentCommand(appointment.Id, "Test", null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Appointment.CannotCancelCompleted");
    }

    [Fact]
    public async Task CancelAppointment_NotFound_ReturnsNotFoundError()
    {
        // Arrange
        var sut = new CancelAppointmentCommandHandler(_repo);
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        // Act
        var result = await sut.Handle(
            new CancelAppointmentCommand(id, null, null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Appointment.NotFound");
    }

    [Fact]
    public async Task ConfirmAppointment_ScheduledAppointment_Succeeds()
    {
        // Arrange
        var sut = new UpdateAppointmentStatusCommandHandler(_repo);
        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        _repo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        // Act
        var result = await sut.Handle(
            new UpdateAppointmentStatusCommand(appointment.Id, AppointmentStatus.Confirmed),
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AppointmentStatus.Confirmed);
        await _repo.Received(1).UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}

public class GetAppointmentByIdQueryHandlerTests
{
    private readonly IAppointmentRepository _repo = Substitute.For<IAppointmentRepository>();

    [Fact]
    public async Task Handle_ExistingAppointment_ReturnsIt()
    {
        // Arrange
        var sut = new GetAppointmentByIdQueryHandler(_repo);
        var appt = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2));
        _repo.GetByIdAsync(appt.Id, Arg.Any<CancellationToken>()).Returns(appt);

        // Act
        var result = await sut.Handle(new GetAppointmentByIdQuery(appt.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(appt.Id);
        result.Value.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        // Arrange
        var sut = new GetAppointmentByIdQueryHandler(_repo);
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        // Act
        var result = await sut.Handle(new GetAppointmentByIdQuery(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Appointment.NotFound");
    }
}

