using FluentAssertions;
using DentFlow.Patients.Domain;

// Not ".Domain": that namespace would shadow "Domain.Gender" in the older handler tests.
namespace DentFlow.Patients.Tests;

/// <summary>People-merge plan B1 / D8: patients are deactivated, not deleted.</summary>
public class PatientStatusTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 9, 30, 0, DateTimeKind.Utc);

    private static Patient NewPatient() => Patient.Create("P-000001", "Selma", "Aganović", null, null, null);

    [Fact]
    public void Deactivate_SetsInactive_AndStampsWhen()
    {
        var patient = NewPatient();

        var result = patient.Deactivate(null, Now);

        result.IsError.Should().BeFalse();
        patient.Status.Should().Be(PatientStatus.Inactive);
        patient.DeactivatedAt.Should().Be(Now);
        patient.DeactivationReason.Should().BeNull();
    }

    [Fact]
    public void Deactivate_StoresTheOptionalReason_Trimmed()
    {
        var patient = NewPatient();

        patient.Deactivate("  Moved to Sarajevo  ", Now);

        patient.DeactivationReason.Should().Be("Moved to Sarajevo");
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ReturnsConflict()
    {
        var patient = NewPatient();
        patient.Deactivate(null, Now);

        var result = patient.Deactivate("again", Now.AddDays(1));

        result.FirstError.Code.Should().Be(PatientErrors.AlreadyInactive.Code);
        patient.DeactivatedAt.Should().Be(Now);
        patient.DeactivationReason.Should().BeNull();
    }

    [Fact]
    public void Reactivate_FromInactive_SetsActive_AndClearsTheStamp()
    {
        var patient = NewPatient();
        patient.Deactivate("Moved", Now);

        var result = patient.Reactivate();

        result.IsError.Should().BeFalse();
        patient.Status.Should().Be(PatientStatus.Active);
        patient.DeactivatedAt.Should().BeNull();
        patient.DeactivationReason.Should().BeNull();
    }

    [Fact]
    public void Reactivate_FromTransferred_IsAllowed()
    {
        var patient = NewPatient();
        patient.UpdateStatus(PatientStatus.Transferred);

        patient.Reactivate().IsError.Should().BeFalse();

        patient.Status.Should().Be(PatientStatus.Active);
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ReturnsConflict()
    {
        NewPatient().Reactivate().FirstError.Code.Should().Be(PatientErrors.AlreadyActive.Code);
    }
}
