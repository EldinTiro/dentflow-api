namespace PearlDesk.Appointments.Domain;

public static class AppointmentStatus
{
    public const string Scheduled = "Scheduled";
    public const string Confirmed = "Confirmed";
    public const string CheckedIn = "CheckedIn";
    public const string InChair = "InChair";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";
    public const string Rescheduled = "Rescheduled";

    public static readonly string[] All =
    [
        Scheduled, Confirmed, CheckedIn, InChair, Completed, Cancelled, NoShow, Rescheduled
    ];

    public static bool IsValid(string status) => All.Contains(status, StringComparer.OrdinalIgnoreCase);
}

public static class AppointmentSource
{
    public const string Staff = "Staff";
    public const string PatientPortal = "PatientPortal";
    public const string OnlineBooking = "OnlineBooking";
}

