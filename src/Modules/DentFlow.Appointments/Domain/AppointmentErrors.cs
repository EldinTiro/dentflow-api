using ErrorOr;

namespace DentFlow.Appointments.Domain;

public static class AppointmentErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Appointment.NotFound", "Appointment was not found.");

    public static readonly Error ProviderConflict =
        Error.Conflict("Appointment.ProviderConflict", "This provider already has an appointment during this time slot. Please select a different time.");

    public static readonly Error InvalidTimeRange =
        Error.Validation("Appointment.InvalidTimeRange", "End time must be after start time.");

    public static readonly Error CannotCancelCompleted =
        Error.Conflict("Appointment.CannotCancelCompleted", "A completed appointment cannot be cancelled.");

    public static readonly Error AppointmentTypeNotFound =
        Error.NotFound("AppointmentType.NotFound", "Appointment type was not found.");

    public static readonly Error ProviderUnavailable =
        Error.Conflict("Appointment.ProviderUnavailable", "This provider has a scheduled leave or block during the selected time. Please choose a different date or another provider.");
}

