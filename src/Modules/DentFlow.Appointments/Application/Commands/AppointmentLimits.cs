namespace DentFlow.Appointments.Application.Commands;

/// <summary>DT-08 / DT-33: bounds that keep a typo from becoming a 24-hour or year-2099 visit.</summary>
public static class AppointmentLimits
{
    public static readonly TimeSpan MaxLength = TimeSpan.FromHours(8);
    public const int BookingHorizonYears = 2;
    public const int MaxNotesLength = 2000;
}
