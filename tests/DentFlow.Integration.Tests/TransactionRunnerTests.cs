using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;
using DentFlow.Staff.Domain;

namespace DentFlow.Integration.Tests;

/// <summary>
/// People-merge plan 2.1: account and profile are written together or not at all.
/// Identity and Staff share one DbContext, so one database transaction covers both.
/// </summary>
[Collection("Integration")]
public class TransactionRunnerTests(DentFlowAppFactory factory)
{
    private async Task<(Guid TenantId, string Email)> SetupAsync()
    {
        var tenantId = await factory.SeedTenantAsync($"tx-{Guid.NewGuid():N}"[..18], "Tx Clinic");
        // Makes sure the Dentist role exists.
        await factory.SeedUserAsync($"seed-{Guid.NewGuid():N}@tx.test", "Seed123!@#abcd", Roles.Staff, tenantId);
        return (tenantId, $"tx-{Guid.NewGuid():N}@clinic.com");
    }

    private async Task<(bool UserExists, bool StaffExists)> ExistsAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await users.FindByEmailAsync(email) is not null,
                await db.Set<StaffMember>().IgnoreQueryFilters().AnyAsync(s => s.Email == email));
    }

    private static async Task<ErrorOr<Guid>> CreateBothAsync(IServiceProvider sp, Guid tenantId, string email)
    {
        var provisioning = sp.GetRequiredService<IUserProvisioningService>();
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var userId = await provisioning.CreateUserAsync(email, "Tx123!@#abcdef", "Ana", "Tx", Roles.Staff, tenantId);
        var staff = StaffMember.Create(StaffType.Dentist, "Ana", "Tx", email, null, null);
        staff.SetTenant(tenantId);
        staff.LinkUser(userId.Value);
        db.Set<StaffMember>().Add(staff);
        await db.SaveChangesAsync();
        return userId.Value;
    }

    [Fact]
    public async Task ExecuteInTransaction_RollsBackBothIdentityAndStaffWrites_WhenTheDelegateFails()
    {
        var (tenantId, email) = await SetupAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<ITransactionRunner>();

            var result = await runner.ExecuteAsync<Guid>(async ct =>
            {
                await CreateBothAsync(scope.ServiceProvider, tenantId, email);
                return Error.Conflict("Test.Fail", "fails after both writes");
            }, CancellationToken.None);

            result.IsError.Should().BeTrue();
        }

        (await ExistsAsync(email)).Should().Be((false, false));
    }

    [Fact]
    public async Task ExecuteInTransaction_RollsBack_WhenTheDelegateThrows()
    {
        var (tenantId, email) = await SetupAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<ITransactionRunner>();

            var act = () => runner.ExecuteAsync<Guid>(async ct =>
            {
                await CreateBothAsync(scope.ServiceProvider, tenantId, email);
                throw new InvalidOperationException("boom");
            }, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        (await ExistsAsync(email)).Should().Be((false, false));
    }

    [Fact]
    public async Task ExecuteInTransaction_Commits_WhenTheDelegateSucceeds()
    {
        var (tenantId, email) = await SetupAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<ITransactionRunner>();

            var result = await runner.ExecuteAsync(ct => CreateBothAsync(scope.ServiceProvider, tenantId, email), CancellationToken.None);

            result.IsError.Should().BeFalse();
        }

        (await ExistsAsync(email)).Should().Be((true, true));
    }
}
