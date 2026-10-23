namespace DentFlow.Domain.Identity;

/// <summary>
/// What a user may do, independent of their job title. Endpoints declare a permission
/// (<c>Policies(Permission.X)</c>); <see cref="RolePermissions"/> decides which roles hold it.
/// Each permission is also registered as an authorization policy of the same name.
/// </summary>
public static class Permission
{
    /// <summary>The operational dashboard: counts of visits, new patients, recalls. No money.</summary>
    public const string DashboardView = "dashboard.view";

    /// <summary>One patient's invoices and balance — what the desk needs at checkout.</summary>
    public const string BillingView = "billing.view";

    /// <summary>Issue an invoice for a visit or plan, and take a payment.</summary>
    public const string BillingCollect = "billing.collect";

    /// <summary>Change money after the fact: edit an invoice, its line items, or void it.</summary>
    public const string FinanceAdjust = "finance.adjust";

    /// <summary>Clinic-level money: revenue, outstanding totals, the full invoice ledger.</summary>
    public const string FinanceView = "finance.view";

    /// <summary>
    /// Add, edit and deactivate staff, and decide who can sign in (grant access, change
    /// role, reset password). Owner only (people-merge plan D2 / R1).
    /// </summary>
    public const string PeopleManage = "people.manage";

    public static readonly IReadOnlyList<string> All =
    [
        DashboardView, BillingView, BillingCollect, FinanceAdjust, FinanceView, PeopleManage,
    ];
}
