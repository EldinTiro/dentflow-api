using DentFlow.Domain.Features;

namespace DentFlow.Application.Common.Interfaces;

public interface IFeatureService
{
    bool IsEnabled(FeatureFlag flag);

    /// <summary>Returns the quota value. -1 means unlimited.</summary>
    int GetQuota(TenantQuota quota);

    IReadOnlyList<FeatureFlag> EnabledFlags();
    IReadOnlyDictionary<string, int> AllQuotas();
}
