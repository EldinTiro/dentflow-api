namespace DentFlow.Tenants.Application;

public record TenantFeaturesResponse(
    string Plan,
    IReadOnlyList<string> Flags,
    IReadOnlyDictionary<string, int> Quotas);
