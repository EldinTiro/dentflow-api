using ErrorOr;

namespace PearlDesk.Appointments.Domain;

public static class AppointmentErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Appointment.NotFound", "Appointment was not found.");

    public static readonly Error ProviderConflict =
        Error.Conflict("Appointment.ProviderConflict", "The provider already has an appointment during this time slot.");

    public static readonly Error InvalidTimeRange =
        Error.Validation("Appointment.InvalidTimeRange", "End time must be after start time.");

    public static readonly Error CannotCancelCompleted =
        Error.Conflict("Appointment.CannotCancelCompleted", "A completed appointment cannot be cancelled.");

    public static readonly Error AppointmentTypeNotFound =
        Error.NotFound("AppointmentType.NotFound", "Appointment type was not found.");
}

