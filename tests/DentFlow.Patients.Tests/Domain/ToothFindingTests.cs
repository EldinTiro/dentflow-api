using FluentAssertions;
using DentFlow.Domain.Clinical;
using DentFlow.Patients.Domain;

// Not ".Domain": that namespace would shadow "Domain.Gender" in the older handler tests.
namespace DentFlow.Patients.Tests;

/// <summary>FDI numbering, used in Bosnia and the rest of Europe (two digits: quadrant, then tooth).</summary>
public class FdiTests
{
    [Theory]
    [InlineData(11)] [InlineData(18)] [InlineData(21)] [InlineData(28)]
    [InlineData(31)] [InlineData(38)] [InlineData(41)] [InlineData(48)]
    public void IsPermanentTooth_AcceptsEveryQuadrant(int tooth) => Fdi.IsPermanentTooth(tooth).Should().BeTrue();

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(10)] [InlineData(19)] [InlineData(20)]
    [InlineData(49)] [InlineData(51)] [InlineData(32 + 50)]
    public void IsPermanentTooth_RejectsAnythingElse(int tooth) => Fdi.IsPermanentTooth(tooth).Should().BeFalse();

    // Treatment items were numbered 1–32 (US "Universal"); the migration converts them with this
    [Theory]
    [InlineData(1, 18)]  // upper right third molar
    [InlineData(8, 11)]  // upper right central incisor
    [InlineData(9, 21)]  // upper left central incisor
    [InlineData(16, 28)] // upper left third molar
    [InlineData(17, 38)] // lower left third molar
    [InlineData(24, 31)] // lower left central incisor
    [InlineData(25, 41)] // lower right central incisor
    [InlineData(32, 48)] // lower right third molar
    public void FromUniversal_MapsToTheSameTooth(int universal, int fdi) => Fdi.FromUniversal(universal).Should().Be(fdi);
}

public class ToothFindingTests
{
    private static readonly Guid PatientId = Guid.NewGuid();

    [Fact]
    public void Create_RecordsTheFinding()
    {
        var result = ToothFinding.Create(PatientId, 36, ToothCondition.Filling, "mod", "Composite");

        result.IsError.Should().BeFalse();
        result.Value.Tooth.Should().Be(36);
        result.Value.Condition.Should().Be(ToothCondition.Filling);
        result.Value.Surfaces.Should().Be("MOD"); // stored upper-case, in the usual M-O-D-B-L order
        result.Value.Notes.Should().Be("Composite");
    }

    [Fact]
    public void Create_OrdersSurfacesTheUsualWay()
    {
        ToothFinding.Create(PatientId, 36, ToothCondition.Caries, "LDO", null).Value.Surfaces.Should().Be("ODL");
    }

    [Theory]
    [InlineData(9)]
    [InlineData(19)]
    [InlineData(55)]
    public void Create_WithAToothThatIsNotFdi_IsRefused(int tooth)
    {
        ToothFinding.Create(PatientId, tooth, ToothCondition.Caries, null, null)
            .FirstError.Code.Should().Be("ToothFinding.InvalidTooth");
    }

    [Fact]
    public void Create_WithAnUnknownCondition_IsRefused()
    {
        ToothFinding.Create(PatientId, 36, "Sparkly", null, null)
            .FirstError.Code.Should().Be("ToothFinding.InvalidCondition");
    }

    [Theory]
    [InlineData("X")]
    [InlineData("MM")]
    public void Create_WithUnknownOrRepeatedSurfaces_IsRefused(string surfaces)
    {
        ToothFinding.Create(PatientId, 36, ToothCondition.Filling, surfaces, null)
            .FirstError.Code.Should().Be("ToothFinding.InvalidSurfaces");
    }

    // A crown or a missing tooth has no surfaces to name
    [Theory]
    [InlineData(ToothCondition.Crown)]
    [InlineData(ToothCondition.Missing)]
    [InlineData(ToothCondition.Implant)]
    [InlineData(ToothCondition.RootCanal)]
    public void Create_WithSurfacesOnAWholeToothCondition_IsRefused(string condition)
    {
        ToothFinding.Create(PatientId, 36, condition, "O", null)
            .FirstError.Code.Should().Be("ToothFinding.SurfacesNotApplicable");
    }

    [Fact]
    public void Create_WithBlankSurfaces_StoresNone()
    {
        ToothFinding.Create(PatientId, 36, ToothCondition.Crown, "  ", null).Value.Surfaces.Should().BeNull();
    }
}
