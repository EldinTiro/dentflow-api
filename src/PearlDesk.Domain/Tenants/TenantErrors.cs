using ErrorOr;

namespace PearlDesk.Domain.Tenants;

public static class TenantErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Tenant.NotFound", "The tenant was not found.");

    public static readonly Error SlugTaken = Error.Conflict(
        "Tenant.SlugTaken", "A tenant with this slug already exists.");

    public static readonly Error Inactive = Error.Forbidden(
        "Tenant.Inactive", "This tenant account is suspended.");

    public static readonly Error SlugInvalid = Error.Validation(
        "Tenant.SlugInvalid", "Slug may only contain lowercase letters, numbers, and hyphens.");
}

