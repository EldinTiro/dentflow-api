namespace DentFlow.Domain.Identity;

/// <summary>
/// The one place that says which role holds which permission
/// (documentation/roles-permissions-proposal-2026-10-22.md §5).
/// Clinic-level money stays with the owner; everyone at the
/// desk can still see and collect a patient's own bill.
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, HashSet<string>> Map = new()
    {
        [Roles.ClinicOwner] = [.. Permission.All],
        [Roles.Dentist] = [Permission.DashboardView, Permission.BillingView, Permission.PatientsManage],
        // Nurses work chairside and at the desk: they see and collect a patient's bill.
        [Roles.DentalNurse] = [Permission.DashboardView, Permission.BillingView, Permission.BillingCollect, Permission.PatientsManage],
        [Roles.Receptionist] = [Permission.DashboardView, Permission.BillingView, Permission.BillingCollect, Permission.PatientsManage],
        // Platform admin: the operational dashboard only; never a clinic's money.
        [Roles.SuperAdmin] = [Permission.DashboardView],
    };

    public static bool Has(string role, string permission) =>
        Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public static IReadOnlyList<string> RolesWith(string permission) =>
        [.. Map.Where(kv => kv.Value.Contains(permission)).Select(kv => kv.Key)];
}
