namespace DentFlow.Appointments.Domain;

public static class AppointmentStatus
{
    public const string Scheduled   = "Scheduled";
    public const string CheckedIn   = "CheckedIn";
    public const string InProgress  = "InProgress";
    public const string Completed   = "Completed";
    public const string Cancelled   = "Cancelled";
    public const string NoShow      = "NoShow";

    /// <summary>Valid transitions: key → allowed next statuses.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>
        {
            [Scheduled]  = [CheckedIn, InProgress, Cancelled, NoShow],
            [CheckedIn]  = [InProgress, Cancelled],
            [InProgress] = [Completed],
        };

    public static bool IsTerminal(string status) =>
        status is Completed or Cancelled or NoShow;

    public static bool CanTransitionTo(string fromStatus, string toStatus) =>
        AllowedTransitions.TryGetValue(fromStatus, out var allowed) && allowed.Contains(toStatus);

    public static bool IsValid(string status) =>
        status is Scheduled or CheckedIn or InProgress or Completed or Cancelled or NoShow;
}

public static class AppointmentSource
{
    public const string Staff = "Staff";
    public const string PatientPortal = "PatientPortal";
    public const string OnlineBooking = "OnlineBooking";
}

