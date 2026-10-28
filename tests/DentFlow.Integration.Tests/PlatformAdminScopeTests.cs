using System.Net;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-18: the platform admin got 403 on some clinic endpoints and an empty 200 on others.
/// Clinic data is for clinic roles (people-merge Part B / B0): every one answers 403.
/// </summary>
[Collection("Integration")]
public class PlatformAdminScopeTests(DentFlowAppFactory factory)
{
    private static readonly Guid Any = Guid.NewGuid();

    public static TheoryData<string> ClinicReads =>
    [
        "/api/v1/appointments",
        $"/api/v1/appointments/{Any}",
        "/api/v1/appointment-types",
        "/api/v1/staff",
        $"/api/v1/staff/{Any}",
        $"/api/v1/staff/{Any}/availability",
        $"/api/v1/staff/{Any}/blocked-times",
        $"/api/v1/patients/{Any}",
        $"/api/v1/patients/{Any}/treatment-plans",
        $"/api/v1/treatment-plans/{Any}",
    ];

    [Theory]
    [MemberData(nameof(ClinicReads))]
    public async Task SuperAdmin_GetsForbidden_OnClinicData(string path)
    {
        var response = await factory.CreateAuthenticatedClient([Roles.SuperAdmin]).GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
