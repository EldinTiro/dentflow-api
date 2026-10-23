using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Tests.Commands;

/// <summary>People-merge plan B2 / D10: deactivating is refused while appointments are still ahead.</summary>
public class PatientStatusCommandHandlerTests
{
    private readonly IPatientRepository _repo = Substitute.For<IPatientRepository>();
    private readonly IAppointmentReader _appointments = Substitute.For<IAppointmentReader>();

    private Patient Existing()
    {
        var patient = Patient.Create("P-000001", "Selma", "Aganović", null, null, null);
        _repo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        return patient;
    }

    [Fact]
    public async Task Deactivate_WithNoUpcomingAppointments_SetsInactive()
    {
        var patient = Existing();
        _appointments.CountUpcomingForPatientAsync(patient.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await new DeactivatePatientCommandHandler(_repo, _appointments)
            .Handle(new DeactivatePatientCommand(patient.Id, "Moved"), default);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(PatientStatus.Inactive);
        patient.DeactivationReason.Should().Be("Moved");
        await _repo.Received(1).UpdateAsync(patient, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_WithUpcomingAppointments_ReturnsConflictWithTheCount_AndChangesNothing()
    {
        var patient = Existing();
        _appointments.CountUpcomingForPatientAsync(patient.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(2);

        var result = await new DeactivatePatientCommandHandler(_repo, _appointments)
            .Handle(new DeactivatePatientCommand(patient.Id, null), default);

        result.FirstError.Code.Should().Be("Patient.HasUpcomingAppointments");
        result.FirstError.Metadata.Should().ContainKey("count").WhoseValue.Should().Be(2);
        patient.Status.Should().Be(PatientStatus.Active);
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_UnknownPatient_ReturnsNotFound()
    {
        var result = await new DeactivatePatientCommandHandler(_repo, _appointments)
            .Handle(new DeactivatePatientCommand(Guid.NewGuid(), null), default);

        result.FirstError.Code.Should().Be(PatientErrors.NotFound.Code);
    }

    [Fact]
    public async Task Deactivate_AlreadyInactive_ReturnsConflict()
    {
        var patient = Existing();
        patient.Deactivate(null, DateTime.UtcNow);

        var result = await new DeactivatePatientCommandHandler(_repo, _appointments)
            .Handle(new DeactivatePatientCommand(patient.Id, null), default);

        result.FirstError.Code.Should().Be(PatientErrors.AlreadyInactive.Code);
    }

    [Fact]
    public async Task Reactivate_Inactive_SetsActive()
    {
        var patient = Existing();
        patient.Deactivate(null, DateTime.UtcNow);

        var result = await new ReactivatePatientCommandHandler(_repo).Handle(new ReactivatePatientCommand(patient.Id), default);

        result.Value.Status.Should().Be(PatientStatus.Active);
        await _repo.Received(1).UpdateAsync(patient, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reactivate_UnknownPatient_ReturnsNotFound()
    {
        var result = await new ReactivatePatientCommandHandler(_repo).Handle(new ReactivatePatientCommand(Guid.NewGuid()), default);

        result.FirstError.Code.Should().Be(PatientErrors.NotFound.Code);
    }
}
