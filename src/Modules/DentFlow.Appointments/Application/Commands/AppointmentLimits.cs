namespace DentFlow.Appointments.Application.Commands;

/// <summary>DT-08 / DT-33: bounds that keep a typo from becoming a 24-hour or year-2099 visit.</summary>
public static class AppointmentLimits
{
    public static readonly TimeSpan MaxLength = TimeSpan.FromHours(8);
    public const int BookingHorizonYears = 2;
    public const int MaxNotesLength = 2000;

    /// <summary>
    /// QA-018 / N-04: a booking or a move may not start in the past — with a little slack for
    /// clock skew and a walk-in booked as they arrive.
    /// </summary>
    public static readonly TimeSpan PastStartSlack = TimeSpan.FromMinutes(10);

    public static bool StartsInThePast(DateTime startAt) => startAt < DateTime.UtcNow - PastStartSlack;

    /// <summary>
    /// DT-09: arrival statuses (checked in, started, finished, no-show) only make sense close
    /// to the visit. The server has no clinic time zone, so "the appointment day" is a window.
    /// </summary>
    public static readonly TimeSpan ArrivalWindowBeforeStart = TimeSpan.FromHours(12);
}
