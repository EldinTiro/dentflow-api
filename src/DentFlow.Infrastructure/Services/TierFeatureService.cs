using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Features;
using DentFlow.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DentFlow.Infrastructure.Services;

/// <summary>
/// Derives the active feature set from the tenant's subscription plan.
/// Plan is loaded once per request (scoped lifetime) and cached in a private field.
/// Resolves tenant via JWT 'tid' claim first, then Finbuckle identifier as fallback.
/// </summary>
internal sealed class TierFeatureService(
    ApplicationDbContext db,
    IMultiTenantContextAccessor multiTenantContextAccessor,
    IHttpContextAccessor httpContextAccessor) : IFeatureService
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

        // Prefer the JWT 'tid' claim — set by the Identity module at login and
        // always present in authenticated requests regardless of dev/prod Finbuckle config.
        var tidClaim = httpContextAccessor.HttpContext?.User.FindFirst("tid")?.Value;
        if (Guid.TryParse(tidClaim, out var tenantIdFromClaim))
        {
            var plan = await db.Tenants
                .Where(t => t.Id == tenantIdFromClaim)
                .Select(t => t.Plan)
                .FirstOrDefaultAsync();

            _cachedPlan = plan ?? "Free";
            return _cachedPlan;
        }

        // Fallback: Finbuckle identifier (works when identifier is a Guid in production).
        var identifier = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;
        if (Guid.TryParse(identifier, out var tenantIdFromFinbuckle))
        {
            var plan = await db.Tenants
                .Where(t => t.Id == tenantIdFromFinbuckle)
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
