namespace DentFlow.Domain.Identity;

/// <summary>
/// The roles a user can hold. A role says what someone may do in the app, not their job
/// title — that is the staff type (Stomatolog, Tehničar…), which also decides who can be
/// booked. Two clinic roles (2026-11-02): the Administrator (code <see cref="ClinicOwner"/>)
/// and everyone else, <see cref="Staff"/> ("Osoblje"). What each may do: <see cref="RolePermissions"/>.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    /// <summary>Administrator: clinic money, people and settings, besides everything Staff does.</summary>
    public const string ClinicOwner = "ClinicOwner";
    /// <summary>Osoblje: the daily work — patients, appointments, a patient's bill.</summary>
    public const string Staff = "Staff";

    public static readonly IReadOnlyList<string> All = [SuperAdmin, ClinicOwner, Staff];

    public static readonly IReadOnlyList<string> TenantRoles = [ClinicOwner, Staff];

    /// <summary>
    /// Roles that no longer exist, and where their users go at startup
    /// (<c>SuperAdminSeeder.SyncRolesAsync</c>). <c>null</c> deactivates the user instead:
    /// a read-only account must not be promoted to a role that can change data.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> Retired = new Dictionary<string, string?>
    {
        ["ClinicAdmin"] = ClinicOwner,
        // Job-title roles, retired 2026-11-02: the title lives on the staff profile
        ["Dentist"] = Staff,
        ["DentalNurse"] = Staff,
        ["Receptionist"] = Staff,
        ["Hygienist"] = Staff,
        ["BillingStaff"] = Staff,
        ["ReadOnly"] = null,
    };
}
