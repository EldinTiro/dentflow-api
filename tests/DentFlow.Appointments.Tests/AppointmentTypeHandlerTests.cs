using FluentAssertions;
using NSubstitute;
using DentFlow.Appointments.Application.Commands;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Tests;

/// <summary>
/// QA pass 2026-10-31 (N-11): a second service type with a name the clinic already uses hit the
/// unique index and came back as a 500.
/// </summary>
public class AppointmentTypeHandlerTests
{
    private readonly IAppointmentTypeRepository _repo = Substitute.For<IAppointmentTypeRepository>();

    private static CreateAppointmentTypeCommand Create(string name = "Filling") => new(name, 45, null, null, false, null);

    [Fact]
    public async Task Create_NameAlreadyUsed_ReturnsDuplicateName()
    {
        _repo.NameExistsAsync("Filling", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new CreateAppointmentTypeCommandHandler(_repo).Handle(Create(), CancellationToken.None);

        result.FirstError.Code.Should().Be("AppointmentType.DuplicateName");
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Create_NewName_IsAdded()
    {
        var result = await new CreateAppointmentTypeCommandHandler(_repo).Handle(Create(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _repo.ReceivedWithAnyArgs(1).AddAsync(default!, default);
    }

    [Fact]
    public async Task Update_NameUsedByAnotherType_ReturnsDuplicateName()
    {
        var type = AppointmentType.Create("Check-up", 30, null, null, false, null);
        _repo.GetByIdAsync(type.Id, Arg.Any<CancellationToken>()).Returns(type);
        _repo.NameExistsAsync("Filling", type.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new UpdateAppointmentTypeCommandHandler(_repo)
            .Handle(new UpdateAppointmentTypeCommand(type.Id, "Filling", 30, null, null, false, null), CancellationToken.None);

        result.FirstError.Code.Should().Be("AppointmentType.DuplicateName");
        await _repo.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }
}
