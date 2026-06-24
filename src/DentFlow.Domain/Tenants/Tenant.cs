namespace DentFlow.Domain.Tenants;

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
    public string? LogoBase64 { get; private set; }

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
    public void UpdateName(string name) => Name = name.Trim();
    public void SetStripeCustomer(string customerId) => StripeCustomerId = customerId;
    public void SetStripeSubscription(string subscriptionId) => StripeSubscriptionId = subscriptionId;
    public void SetPlan(string plan, DateTime? expiresAt)
    {
        Plan = plan;
        PlanExpiresAt = expiresAt;
    }

    // ── Working hours ───────────────────────────────────────────────────────
    public TimeOnly WorkdayStart { get; private set; } = new(8, 0);
    public TimeOnly WorkdayEnd { get; private set; } = new(22, 30);
    public int SlotDurationMinutes { get; private set; } = 30;

    /// <summary>
    /// JSON-serialised per-day schedule. Null means "use global WorkdayStart/End for all open days."
    /// </summary>
    public string? WeeklyScheduleJson { get; private set; }

    public void SetLogo(string? logoBase64) => LogoBase64 = logoBase64;

    public void UpdateWorkingHours(TimeOnly start, TimeOnly end, int slotDurationMinutes)
    {
        WorkdayStart = start;
        WorkdayEnd = end;
        SlotDurationMinutes = slotDurationMinutes;
    }

    public void UpdateWeeklySchedule(string scheduleJson, int slotDurationMinutes)
    {
        WeeklyScheduleJson = scheduleJson;
        SlotDurationMinutes = slotDurationMinutes;
    }
}

