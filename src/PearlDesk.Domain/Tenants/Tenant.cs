namespace PearlDesk.Domain.Tenants;

/// <summary>
/// Root tenant record. Not tenant-scoped itself — managed by SuperAdmin only.
/// </summary>
public class Tenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Plan { get; private set; } = "Free";
    public DateTime? PlanExpiresAt { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? OnboardingCompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Tenant() { }

    public static Tenant Create(string slug, string name)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = slug.ToLowerInvariant().Trim(),
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void CompleteOnboarding() => OnboardingCompletedAt = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SetStripeCustomer(string customerId) => StripeCustomerId = customerId;
    public void SetStripeSubscription(string subscriptionId) => StripeSubscriptionId = subscriptionId;
    public void SetPlan(string plan, DateTime? expiresAt)
    {
        Plan = plan;
        PlanExpiresAt = expiresAt;
    }
}

