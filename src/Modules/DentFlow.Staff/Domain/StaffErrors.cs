using ErrorOr;

namespace DentFlow.Staff.Domain;

public static class StaffErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Staff.NotFound", "Staff member was not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("Staff.AlreadyExists", "A staff member with this email already exists.");

    public static readonly Error InvalidStaffType =
        Error.Validation("Staff.InvalidStaffType", "The provided staff type is not valid.");

    public static readonly Error CannotDeleteActive =
        Error.Conflict("Staff.CannotDeleteActive", "Cannot delete an active staff member. Deactivate first.");

    public static readonly Error AvailabilityNotFound =
        Error.NotFound("Staff.AvailabilityNotFound", "Staff availability slot was not found.");

    public static readonly Error ConflictingAvailabilitySlot =
        Error.Conflict("Staff.ConflictingAvailabilitySlot", "An availability slot already exists for this day that overlaps with the requested time range.");

    // ── Sign-in access (people-merge plan 1.1) ──────────────────────────

    public static readonly Error AlreadyHasAccess =
        Error.Conflict("Staff.AlreadyHasAccess", "This person can already sign in.");

    public static readonly Error Inactive =
        Error.Conflict("Staff.Inactive", "This person is inactive. Reactivate them first.");

    public static readonly Error EmailRequiredForAccess =
        Error.Validation("Staff.EmailRequiredForAccess", "An email address is needed to sign in. Add one to the profile first.");

    public static readonly Error EmailLockedByAccess =
        Error.Validation("Staff.EmailLockedByAccess", "This email is the person's sign-in email and can't be changed here.");

    public static readonly Error InvalidAccessRole =
        Error.Validation("Staff.InvalidAccessRole", "Choose one of the clinic roles: owner, dentist, dental nurse or receptionist.");

    // ── Deactivate instead of delete (people-merge plan B6) ─────────────

    public static readonly Error AlreadyInactive =
        Error.Conflict("Staff.AlreadyInactive", "This person is already inactive.");

    public static readonly Error AlreadyActive =
        Error.Conflict("Staff.AlreadyActive", "This person is already active.");

    /// <summary>The count travels as metadata so the client can say how many (D10).</summary>
    public static Error HasUpcomingAppointments(int count) =>
        Error.Conflict(
            "Staff.HasUpcomingAppointments",
            $"This person has {count} upcoming appointment(s). Cancel or move them first.",
            new Dictionary<string, object> { ["count"] = count });

    public static readonly Error HasHistory =
        Error.Conflict("Staff.HasHistory", "This person has appointments. Deactivate them instead.");

    public static readonly Error BlockedTimeNotFound =
        Error.NotFound("Staff.BlockedTimeNotFound", "Staff blocked time was not found.");
}

