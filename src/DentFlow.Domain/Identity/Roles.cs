﻿namespace DentFlow.Domain.Identity;

/// <summary>
/// The roles a user can hold. Kept deliberately small for small clinics:
/// the owner, the dentists, dental nurses (chairside and front desk) and reception.
/// What each role may do lives in <see cref="RolePermissions"/>.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string ClinicOwner = "ClinicOwner";
    public const string Dentist = "Dentist";
    /// <summary>Medicinska sestra / tehničar: chairside assistance and front-desk work.</summary>
    public const string DentalNurse = "DentalNurse";
    public const string Receptionist = "Receptionist";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin, ClinicOwner, Dentist, DentalNurse, Receptionist
    ];

    public static readonly IReadOnlyList<string> TenantRoles =
    [
        ClinicOwner, Dentist, DentalNurse, Receptionist
    ];

    /// <summary>
    /// Roles that no longer exist, and where their users go at startup
    /// (<c>SuperAdminSeeder.SyncRolesAsync</c>). <c>null</c> deactivates the user instead:
    /// a read-only account must not be promoted to a role that can change data.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> Retired = new Dictionary<string, string?>
    {
        ["ClinicAdmin"] = ClinicOwner,
        ["Hygienist"] = DentalNurse,
        ["BillingStaff"] = Receptionist,
        ["ReadOnly"] = null,
    };
}
