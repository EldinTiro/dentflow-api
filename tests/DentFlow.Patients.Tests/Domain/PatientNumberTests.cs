using FluentAssertions;
using DentFlow.Patients.Domain;

// Not ".Domain": that namespace would shadow "Domain.Gender" in the older handler tests.
namespace DentFlow.Patients.Tests;

/// <summary>
/// A new patient's card number is the clinic's highest number + 1, in the plain numeric
/// format of a paper kartoteka ("3453"), not "P-000123". Old "P-000050" numbers still
/// count as 50, so a clinic that used them carries on from 51 instead of starting over.
/// </summary>
public class PatientNumberTests
{
    [Fact]
    public void NextNumber_FirstPatientOfAClinic_Is1()
    {
        Patient.NextNumber([]).Should().Be("1");
    }

    [Fact]
    public void NextNumber_IsTheHighestNumberPlusOne_NotTheCount()
    {
        Patient.NextNumber(["2314", "4107", "22"]).Should().Be("4108");
    }

    [Fact]
    public void NextNumber_ComparesNumbers_NotText()
    {
        Patient.NextNumber(["999", "1000"]).Should().Be("1001");
    }

    [Fact]
    public void NextNumber_ContinuesAfterTheOldPrefixedFormat()
    {
        Patient.NextNumber(["P-000050", "p-000007"]).Should().Be("51");
    }

    [Fact]
    public void NextNumber_TakesTheHighestAcrossBothFormats()
    {
        Patient.NextNumber(["P-000050", "3453"]).Should().Be("3454");
    }

    [Fact]
    public void NextNumber_IgnoresNumbersItCannotRead()
    {
        Patient.NextNumber(["1963-2", "K-12", "3769DŽINO", "  ", "12"]).Should().Be("13");
    }

    [Fact]
    public void NextNumber_ReadsLeadingZerosAndSpaces()
    {
        Patient.NextNumber(["0357", " 400 "]).Should().Be("401");
    }

    [Fact]
    public void NextNumber_SurvivesNumbersTooLongForAnInt()
    {
        Patient.NextNumber(["99999999999999999999", "5"]).Should().Be("100000000000000000000");
    }
}
