using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Features;
using DentFlow.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Derives the active feature set from the tenant's subscription plan.
/// Plan is loaded once per request (scoped lifetime) and cached in a private field.
/// </summary>
internal sealed class TierFeatureService(
    ApplicationDbContext db,
    IMultiTenantContextAccessor multiTenantContextAccessor) : IFeatureService
{
    private static readonly HashSet<FeatureFlag> ProFlags =
    [
        FeatureFlag.MfaEnforcement,
        FeatureFlag.BulkPatientImport,
        FeatureFlag.RecurringAppointments,
        FeatureFlag.OnlineBooking,
        FeatureFlag.InsuranceClaims,
        FeatureFlag.PaymentGatewayIntegration,
        FeatureFlag.DocumentStorage,
        FeatureFlag.SmsNotifications,
        FeatureFlag.CustomEmailTemplates,
        FeatureFlag.AdvancedReporting,
        FeatureFlag.DataExport,
    ];

    private static readonly HashSet<FeatureFlag> EnterpriseFlags =
    [
        ..ProFlags,
        FeatureFlag.SsoOidc,
        FeatureFlag.MultiLocation,
        FeatureFlag.PatientPortal,
        FeatureFlag.WaitlistManagement,
        FeatureFlag.AutomatedInvoicing,
        FeatureFlag.CustomReports,
    ];

    private static readonly Dictionary<string, (HashSet<FeatureFlag> Flags, Dictionary<TenantQuota, int> Quotas)> TierMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Free"]       = ([], new() { [TenantQuota.MaxStaffCount] = 3,  [TenantQuota.StorageLimitGb] = 0  }),
            ["Pro"]        = (ProFlags, new() { [TenantQuota.MaxStaffCount] = 15, [TenantQuota.StorageLimitGb] = 5  }),
            ["Enterprise"] = (EnterpriseFlags, new() { [TenantQuota.MaxStaffCount] = -1, [TenantQuota.StorageLimitGb] = -1 }),
        };

    private string? _cachedPlan;

    private async ValueTask<string> ResolveplanAsync()
    {
        if (_cachedPlan is not null)
            return _cachedPlan;

        var identifier = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;
        if (Guid.TryParse(identifier, out var tenantId))
        {
            var plan = await db.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.Plan)
                .FirstOrDefaultAsync();

            _cachedPlan = plan ?? "Free";
        }
        else
        {
            _cachedPlan = "Free";
        }

        return _cachedPlan;
    }

    // Synchronous convenience — used for in-handler checks that run after any async setup.
    // Callers that need to guard at handler entry should await ResolveplanAsync() first.
    private string ResolvePlanSync() =>
        _cachedPlan ?? ResolveplanAsync().AsTask().GetAwaiter().GetResult();

    public bool IsEnabled(FeatureFlag flag)
    {
        var plan = ResolvePlanSync();
        return TierMap.TryGetValue(plan, out var tier) && tier.Flags.Contains(flag);
    }

    public int GetQuota(TenantQuota quota)
    {
        var plan = ResolvePlanSync();
        return TierMap.TryGetValue(plan, out var tier) && tier.Quotas.TryGetValue(quota, out var value)
            ? value
            : 0;
    }

    public IReadOnlyList<FeatureFlag> EnabledFlags()
    {
        var plan = ResolvePlanSync();
        return TierMap.TryGetValue(plan, out var tier)
            ? tier.Flags.ToList()
            : [];
    }

    public IReadOnlyDictionary<string, int> AllQuotas()
    {
        var plan = ResolvePlanSync();
        return TierMap.TryGetValue(plan, out var tier)
            ? tier.Quotas.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
            : new Dictionary<string, int>();
    }
}
