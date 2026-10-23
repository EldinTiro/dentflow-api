using DentFlow.Domain.Identity;
using FluentAssertions;

namespace DentFlow.Identity.Tests;

/// <summary>
/// The single role → permission map (documentation/roles-permissions-proposal-2026-10-22.md).
/// Simplified role set: Clinic owner, Dentist, Dental nurse, Receptionist (+ platform SuperAdmin).
/// Clinic-level money stays with the owner.
/// </summary>
public class RolePermissionsTests
{
    [Fact]
    public void All_IsTheSimplifiedRoleSet() =>
        Roles.All.Should().BeEquivalentTo(
            [Roles.SuperAdmin, Roles.ClinicOwner, Roles.Dentist, Roles.DentalNurse, Roles.Receptionist]);

    [Fact]
    public void TenantRoles_AreEveryRoleExceptThePlatformAdmin() =>
        Roles.TenantRoles.Should().BeEquivalentTo(
            [Roles.ClinicOwner, Roles.Dentist, Roles.DentalNurse, Roles.Receptionist]);

    [Fact]
    public void RolesWith_FinanceView_IsOnlyTheOwner() =>
        RolePermissions.RolesWith(Permission.FinanceView).Should().BeEquivalentTo([Roles.ClinicOwner]);

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    public void Has_FinanceView_IsFalseForClinicalAndFrontDeskStaff(string role) =>
        RolePermissions.Has(role, Permission.FinanceView).Should().BeFalse();

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    public void Has_BillingView_IsTrueSoTheDeskCanSeeAPatientsBill(string role) =>
        RolePermissions.Has(role, Permission.BillingView).Should().BeTrue();

    [Theory]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    public void Has_BillingCollect_IsTrueForWhoeverTakesPayments(string role) =>
        RolePermissions.Has(role, Permission.BillingCollect).Should().BeTrue();

    [Fact]
    public void Has_EveryPermission_IsTrueForTheOwner() =>
        Permission.All.Should().OnlyContain(p => RolePermissions.Has(Roles.ClinicOwner, p));

    [Fact]
    public void RolesWith_AnyPermission_OnlyReturnsRolesThatExist() =>
        Permission.All.SelectMany(RolePermissions.RolesWith).Should().OnlyContain(r => Roles.All.Contains(r));

    [Fact]
    public void RolesWith_AnyPermission_IsNeverEmpty() =>
        Permission.All.Should().OnlyContain(p => RolePermissions.RolesWith(p).Count > 0);

    // Users still holding a removed role are moved at startup; null = deactivate.
    [Fact]
    public void Retired_MapsEachRemovedRole() =>
        Roles.Retired.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["ClinicAdmin"] = Roles.ClinicOwner,
            ["Hygienist"] = Roles.DentalNurse,
            ["BillingStaff"] = Roles.Receptionist,
            ["ReadOnly"] = null,
        });

    [Fact]
    public void Retired_NeverOverlapsCurrentRoles() =>
        Roles.Retired.Keys.Should().NotIntersectWith(Roles.All);

    // ── People (documentation/people-merge-plan-2026-10-23.md, D2 / R1) ──────

    [Fact]
    public void ClinicOwner_HasPeopleManage() =>
        RolePermissions.Has(Roles.ClinicOwner, Permission.PeopleManage).Should().BeTrue();

    [Theory]
    [InlineData(Roles.Dentist)]
    [InlineData(Roles.DentalNurse)]
    [InlineData(Roles.Receptionist)]
    [InlineData(Roles.SuperAdmin)]
    public void OtherRoles_DoNotHavePeopleManage(string role) =>
        RolePermissions.Has(role, Permission.PeopleManage).Should().BeFalse();
}
