using System.Net;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>
/// DT-14: page=0, page=-1 and pageSize=-1 ended in 500 on patients, staff and
/// appointments (a negative Skip/Take), while other lists clamped them.
/// </summary>
[Collection("Integration")]
public class PagingTests(DentFlowAppFactory factory)
{
    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var list in new[] { "/api/v1/patients", "/api/v1/staff", "/api/v1/appointments" })
            foreach (var query in new[] { "page=0", "page=-1", "pageSize=-1", "pageSize=0", "page=abc", "pageSize=100000" })
                data.Add(list, query);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task OddPagingValues_AreClamped_NotA500(string list, string query)
    {
        var response = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).GetAsync($"{list}?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("dateFrom=notadate")]
    [InlineData("dateTo=2026-13-45")]
    public async Task AnUnreadableDateFilter_Is400_NotSilentlyIgnored(string query)
    {
        var response = await factory.CreateAuthenticatedClient([Roles.ClinicOwner]).GetAsync($"/api/v1/appointments?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
