using FluentAssertions;
using NSubstitute;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Patients.Application.Commands;
using DentFlow.Patients.Application.Interfaces;
using DentFlow.Patients.Domain;
using DentFlow.Patients.Tests.Support;

namespace DentFlow.Patients.Tests.Commands;

/// <summary>People-merge plan B4 / D9: only a duplicate with no history may be deleted.</summary>
public class DeletePatientCommandHandlerTests
{
    private readonly IPatientRepository _repo = Substitute.For<IPatientRepository>();
    private readonly IPatientHistoryReader _history = Substitute.For<IPatientHistoryReader>();

    private Patient Existing()
    {
        var patient = Patient.Create("P-000009", "Dupli", "Kat", null, null, null);
        _repo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        return patient;
    }

    [Fact]
    public async Task Handle_PatientWithoutHistory_SoftDeletes()
    {
        var patient = Existing();
        _history.HasHistoryAsync(patient.Id, true, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new DeletePatientCommandHandler(_repo, _history, new PassThroughTransactionRunner()).Handle(new DeletePatientCommand(patient.Id), default);

        result.IsError.Should().BeFalse();
        await _repo.Received(1).SoftDeleteAsync(patient, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PatientWithHistory_ReturnsConflict_AndDeletesNothing()
    {
        var patient = Existing();
        _history.HasHistoryAsync(patient.Id, true, Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeletePatientCommandHandler(_repo, _history, new PassThroughTransactionRunner()).Handle(new DeletePatientCommand(patient.Id), default);

        result.FirstError.Code.Should().Be(PatientErrors.HasHistory.Code);
        await _repo.DidNotReceive().SoftDeleteAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownPatient_ReturnsNotFound()
    {
        var result = await new DeletePatientCommandHandler(_repo, _history, new PassThroughTransactionRunner()).Handle(new DeletePatientCommand(Guid.NewGuid()), default);

        result.FirstError.Code.Should().Be(PatientErrors.NotFound.Code);
    }

    // Client request 2026-11-10: someone booked who never came has no card; missed visits
    // are not history for them, and go with the patient
    [Fact]
    public async Task Handle_PatientWithoutCard_AsksWithoutMissedVisits_AndRemovesThem()
    {
        var patient = Patient.Create(null, "Anita", "Zaimović", null, null, null);
        _repo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        _history.HasHistoryAsync(patient.Id, false, Arg.Any<CancellationToken>()).Returns(false);

        var result = await new DeletePatientCommandHandler(_repo, _history, new PassThroughTransactionRunner()).Handle(new DeletePatientCommand(patient.Id), default);

        result.IsError.Should().BeFalse();
        await _history.Received(1).RemoveMissedVisitsAsync(patient.Id, Arg.Any<CancellationToken>());
        await _repo.Received(1).SoftDeleteAsync(patient, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PatientWithCard_KeepsMissedVisits()
    {
        var patient = Existing();
        _history.HasHistoryAsync(patient.Id, true, Arg.Any<CancellationToken>()).Returns(false);

        await new DeletePatientCommandHandler(_repo, _history, new PassThroughTransactionRunner()).Handle(new DeletePatientCommand(patient.Id), default);

        await _history.DidNotReceive().RemoveMissedVisitsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
