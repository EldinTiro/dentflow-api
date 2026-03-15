﻿using FluentAssertions;
using NSubstitute;
using PearlDesk.Patients.Application;
using PearlDesk.Patients.Application.Commands;
using PearlDesk.Patients.Application.Interfaces;
using PearlDesk.Patients.Application.Queries;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Tests.Commands;

public class CreatePatientCommandHandlerTests
{
    private readonly IPatientRepository _repo = Substitute.For<IPatientRepository>();
    private readonly CreatePatientCommandHandler _sut;

    public CreatePatientCommandHandlerTests()
    {
        _sut = new CreatePatientCommandHandler(_repo);
    }

    private static CreatePatientCommand BuildCommand(string? email = null) => new(
        FirstName: "Jane",
        LastName: "Doe",
        PreferredName: null,
        DateOfBirth: new DateOnly(1990, 5, 15),
        Gender: Domain.Gender.Female,
        Email: email,
        PhoneMobile: "+1555000001",
        PhoneHome: null,
        PhoneWork: null,
        PreferredContactMethod: Domain.ContactMethod.Mobile,
        AddressLine1: "123 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateProvince: "IL",
        PostalCode: "62701",
        CountryCode: "US",
        Occupation: null,
        PreferredProviderId: null,
        SmsOptIn: true,
        EmailOptIn: true,
        ReferredBySource: null,
        Notes: null);

    [Fact]
    public async Task Handle_NewPatient_CreatesAndReturnsResponse()
    {
        // Arrange
        _repo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns((Patient?)null);
        _repo.GeneratePatientNumberAsync(Arg.Any<CancellationToken>())
             .Returns("P-000001");

        // Act
        var result = await _sut.Handle(BuildCommand("jane@test.com"), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Doe");
        result.Value.PatientNumber.Should().Be("P-000001");

        await _repo.Received(1).AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictError()
    {
        // Arrange
        var existing = Patient.Create("P-000001", "Existing", "Patient", null, "jane@test.com", null);
        _repo.GetByEmailAsync("jane@test.com", Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.Handle(BuildCommand("jane@test.com"), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Patient.AlreadyExists");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoEmail_DoesNotCheckForDuplicate()
    {
        // Arrange
        _repo.GeneratePatientNumberAsync(Arg.Any<CancellationToken>()).Returns("P-000002");

        // Act
        var result = await _sut.Handle(BuildCommand(null), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _repo.DidNotReceive().GetByEmailAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}

public class GetPatientByIdQueryHandlerTests
{
    private readonly IPatientRepository _repo = Substitute.For<IPatientRepository>();
    private readonly GetPatientByIdQueryHandler _sut;

    public GetPatientByIdQueryHandlerTests()
    {
        _sut = new GetPatientByIdQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingPatient_ReturnsPatient()
    {
        // Arrange
        var patient = Patient.Create("P-000001", "John", "Doe", null, null, null);
        _repo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        var result = await _sut.Handle(new GetPatientByIdQuery(patient.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(patient.Id);
        result.Value.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        var result = await _sut.Handle(new GetPatientByIdQuery(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Patient.NotFound");
    }
}

public class DeletePatientCommandHandlerTests
{
    private readonly IPatientRepository _repo = Substitute.For<IPatientRepository>();
    private readonly DeletePatientCommandHandler _sut;

    public DeletePatientCommandHandlerTests()
    {
        _sut = new DeletePatientCommandHandler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingPatient_SoftDeletes()
    {
        // Arrange
        var patient = Patient.Create("P-000001", "John", "Doe", null, null, null);
        _repo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        // Act
        var result = await _sut.Handle(new DeletePatientCommand(patient.Id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _repo.Received(1).SoftDeleteAsync(patient, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        var result = await _sut.Handle(new DeletePatientCommand(id), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Patient.NotFound");
        await _repo.DidNotReceive().SoftDeleteAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }
}

