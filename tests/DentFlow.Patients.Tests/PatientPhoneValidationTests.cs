using FluentAssertions;
using DentFlow.Patients.Application.Commands;

namespace DentFlow.Patients.Tests;

/// <summary>QA-019: "+387" + "12" was saved as a mobile number.</summary>
public class PatientPhoneValidationTests
{
    private static CreatePatientCommand Create(string? mobile) => new(
        null, "Selma", "Aganović", null, null, null, null, null,
        mobile, null, null, null, null, null, null, null, null, null, null, null,
        false, false, null, null);

    private static UpdatePatientCommand Update(string? mobile) => new(
        Guid.NewGuid(), "Selma", "Aganović", null, null, null, null, null, null,
        mobile, null, null, null, null, null, null, null, null, null, null, null,
        false, false, null);

    [Theory]
    [InlineData("+38712")]
    [InlineData("12345")]
    [InlineData("+387 61 abc 123")]
    public void Create_RejectsAnImplausibleNumber(string phone) =>
        new CreatePatientCommandValidator().Validate(Create(phone)).Errors
            .Should().ContainSingle(e => e.PropertyName == "PhoneMobile");

    [Theory]
    [InlineData("+38761542123")]
    [InlineData("+387 61 542 123")]
    [InlineData("061-542-123")]
    [InlineData(null)]
    public void Create_AcceptsAPlausibleNumberOrNone(string? phone) =>
        new CreatePatientCommandValidator().Validate(Create(phone)).IsValid.Should().BeTrue();

    [Fact]
    public void Update_RejectsAnImplausibleNumber() =>
        new UpdatePatientCommandValidator().Validate(Update("+38712")).Errors
            .Should().ContainSingle(e => e.PropertyName == "PhoneMobile");
}
