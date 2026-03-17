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

    public static readonly Error BlockedTimeNotFound =
        Error.NotFound("Staff.BlockedTimeNotFound", "Staff blocked time was not found.");
}

