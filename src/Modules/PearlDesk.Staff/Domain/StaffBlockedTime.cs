using PearlDesk.Domain.Common;

namespace PearlDesk.Staff.Domain;

public class StaffBlockedTime : TenantAuditableEntity
{
    public Guid StaffMemberId { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public StaffMember StaffMember { get; private set; } = default!;

    private StaffBlockedTime() { }

    public static StaffBlockedTime Create(
        Guid staffMemberId,
        DateTime startAt,
        DateTime endAt,
        string? reason = null,
        string? notes = null)
    {
        return new StaffBlockedTime
        {
            StaffMemberId = staffMemberId,
            StartAt = startAt,
            EndAt = endAt,
            Reason = reason,
            Notes = notes
        };
    }
}

