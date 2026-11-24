using FluentAssertions;
using DentFlow.Tenants.Application.Commands;

namespace DentFlow.Tenants.Tests;

/// <summary>
/// A clinic's slug is its subdomain. The website lives on mydentflow.com and www, the app on
/// app.mydentflow.com (user decision 2026-11-13), so those names can never be a clinic's.
/// </summary>
public class CreateTenantCommandValidatorTests
{
    private static CreateTenantCommand With(string slug) =>
        new(slug, "Clinic", "Free", "owner@clinic.ba", "Ana", "Babić");

    [Theory]
    [InlineData("app")]
    [InlineData("www")]
    [InlineData("api")]
    [InlineData("admin")]
    [InlineData("mail")]
    public void Validate_ReservedSlug_IsRefused(string slug)
    {
        var result = new CreateTenantCommandValidator().Validate(With(slug));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug" && e.ErrorMessage.Contains("reserved"));
    }

    [Fact]
    public void Validate_OrdinarySlug_IsAccepted()
    {
        new CreateTenantCommandValidator().Validate(With("bright-smile")).IsValid.Should().BeTrue();
    }
}
