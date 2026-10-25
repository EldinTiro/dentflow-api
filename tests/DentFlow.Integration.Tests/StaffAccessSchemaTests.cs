using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>People-merge plan 1.3: one account belongs to at most one profile per clinic.</summary>
[Collection("Integration")]
public class StaffAccessSchemaTests(DentFlowAppFactory factory)
{
    private static StaffMember Linked(Guid tenantId, Guid userId, string email)
    {
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Schema", email, null, null);
        staff.SetTenant(tenantId);
        staff.LinkUser(userId).IsError.Should().BeFalse();
        return staff;
    }

    [Fact]
    public async Task TwoProfilesInOneClinic_CannotLinkTheSameUser()
    {
        var tenantId = await factory.SeedTenantAsync($"schema-{Guid.NewGuid():N}"[..20], "Schema Clinic");
        var userId = Guid.NewGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Set<StaffMember>().AddRange(
            Linked(tenantId, userId, $"a-{Guid.NewGuid():N}@clinic.com"),
            Linked(tenantId, userId, $"b-{Guid.NewGuid():N}@clinic.com"));
        var save = () => db.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>("the database must refuse a second profile for the same account");
    }
}
