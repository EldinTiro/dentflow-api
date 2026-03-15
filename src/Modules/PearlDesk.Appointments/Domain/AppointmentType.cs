using PearlDesk.Domain.Common;

namespace PearlDesk.Appointments.Domain;

public class AppointmentType : TenantAuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int DefaultDurationMinutes { get; private set; }
    public string? ColorHex { get; private set; }
    public bool IsBookableOnline { get; private set; }
    public bool RequiresNewPatientForm { get; private set; }
    public int SortOrder { get; private set; }

    private AppointmentType() { }

    public static AppointmentType Create(
        string name,
        int defaultDurationMinutes,
        string? description = null,
        string? colorHex = null,
        bool isBookableOnline = false)
    {
        return new AppointmentType
        {
            Name = name,
            DefaultDurationMinutes = defaultDurationMinutes,
            Description = description,
            ColorHex = colorHex ?? "#6B7280",
            IsBookableOnline = isBookableOnline
        };
    }

    public void Update(string name, int defaultDurationMinutes, string? description, string? colorHex, bool isBookableOnline)
    {
        Name = name;
        DefaultDurationMinutes = defaultDurationMinutes;
        Description = description;
        ColorHex = colorHex;
        IsBookableOnline = isBookableOnline;
        SetUpdated();
    }
}

