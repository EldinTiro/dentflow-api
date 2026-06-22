using ErrorOr;

namespace DentFlow.Application.Common;

public static class FeatureErrors
{
    public static readonly Error NotAvailableOnCurrentPlan = Error.Forbidden(
        "Feature.NotAvailableOnCurrentPlan",
        "This feature is not available on your current plan. Please upgrade to access it.");

    public static readonly Error QuotaExceeded = Error.Forbidden(
        "Feature.QuotaExceeded",
        "You have reached the limit for your current plan. Please upgrade to increase your quota.");
}
