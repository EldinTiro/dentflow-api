using DentFlow.Domain.Identity;
using FluentAssertions;

namespace DentFlow.Identity.Tests;

/// <summary>
/// The single role → permission map. Two clinic roles (user decision 2026-11-02): the role
/// says what someone may do in the app, not their job title — that is the staff type.
/// Administrator (code ClinicOwner): everything, including clinic money, people and settings.
/// Staff ("Osoblje"): the daily work — patients, appointments, a patient's bill.
/// </summary>
public class RolePermissionsTests
{
    [Fact]
    public void All_IsThePlatformAdminAndTheTwoClinicRoles() =>
        Roles.All.Should().BeEquivalentTo([Roles.SuperAdmin, Roles.ClinicOwner, Roles.Staff]);

    [Fact]
    public void TenantRoles_AreAdministratorAndStaff() =>
        Roles.TenantRoles.Should().BeEquivalentTo([Roles.ClinicOwner, Roles.Staff]);

    [Fact]
    public void Has_EveryPermission_IsTrueForTheAdministrator() =>
        Permission.All.Should().OnlyContain(p => RolePermissions.Has(Roles.ClinicOwner, p));

    [Theory]
    [InlineData(Permission.DashboardView)]
    [InlineData(Permission.PatientsManage)]
    [InlineData(Permission.BillingView)]
    [InlineData(Permission.BillingCollect)]
    public void Staff_HasTheDailyWork(string permission) =>
        RolePermissions.Has(Roles.Staff, permission).Should().BeTrue();

    [Theory]
    [InlineData(Permission.FinanceView)]
    [InlineData(Permission.FinanceAdjust)]
    [InlineData(Permission.PeopleManage)]
    public void Staff_HasNoClinicMoneyOrPeople(string permission) =>
        RolePermissions.Has(Roles.Staff, permission).Should().BeFalse();

    [Fact]
    public void RolesWith_FinanceView_IsOnlyTheAdministrator() =>
        RolePermissions.RolesWith(Permission.FinanceView).Should().BeEquivalentTo([Roles.ClinicOwner]);

    [Fact]
    public void RolesWith_AnyPermission_OnlyReturnsRolesThatExist() =>
        Permission.All.SelectMany(RolePermissions.RolesWith).Should().OnlyContain(r => Roles.All.Contains(r));

    [Fact]
    public void RolesWith_AnyPermission_IsNeverEmpty() =>
        Permission.All.Should().OnlyContain(p => RolePermissions.RolesWith(p).Count > 0);

    // The platform admin does not handle a clinic's patients or money.
    [Fact]
    public void SuperAdmin_HasOnlyTheDashboard() =>
        Permission.All.Where(p => RolePermissions.Has(Roles.SuperAdmin, p)).Should().Equal(Permission.DashboardView);

    // Users still holding a removed role are moved at startup; null = deactivate.
    [Fact]
    public void Retired_MovesEveryTitleRoleToStaff() =>
        Roles.Retired.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["ClinicAdmin"] = Roles.ClinicOwner,
            ["Dentist"] = Roles.Staff,
            ["DentalNurse"] = Roles.Staff,
            ["Receptionist"] = Roles.Staff,
            ["Hygienist"] = Roles.Staff,
            ["BillingStaff"] = Roles.Staff,
            ["ReadOnly"] = null,
        });

    [Fact]
    public void Retired_NeverOverlapsCurrentRoles() =>
        Roles.Retired.Keys.Should().NotIntersectWith(Roles.All);
}
