using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// People-merge plan R1 / step 0.3: adding, editing and removing staff is the clinic
/// owner's job (<see cref="Permission.PeopleManage"/>). Reading staff stays open to
/// every clinic role — the desk needs the provider list.
/// </summary>
[Collection("Integration")]
public class StaffAccessRulesTests(DentFlowAppFactory factory)
{
    private static object NewStaff() => new
    {
        staffType = "Dentist", firstName = "Ana", lastName = "Rules",
        email = $"rules-{Guid.NewGuid():N}@clinic.com", colorHex = "#3B82F6"
    };

    [Fact]
    public async Task CreateStaff_AsOwner_Returns201()
    {
        var response = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).PostAsJsonAsync("/api/v1/staff", NewStaff());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    [InlineData(Roles.SuperAdmin)]
    public async Task CreateStaff_WithoutPeopleManage_Returns403(string role)
    {
        var response = await factory.CreateAuthenticatedClient([role]).PostAsJsonAsync("/api/v1/staff", NewStaff());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    public async Task ListStaff_AsAnyClinicRole_Returns200(string role)
    {
        var response = await factory.CreateAuthenticatedClient([role]).GetAsync("/api/v1/staff");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
