using ErrorOr;

namespace DentFlow.Appointments.Domain;

public static class AppointmentErrors
{
    public static readonly Error ProviderNotFound =
        Error.NotFound("Staff.NotFound", "The selected provider was not found.");

    public static readonly Error ProviderInactive =
        Error.Conflict("Appointment.ProviderInactive", "This provider is inactive and can't be booked.");

    public static readonly Error ProviderNotBookable =
        Error.Conflict("Appointment.ProviderNotBookable", "This staff member doesn't take appointments.");

    public static readonly Error PatientConflict =
        Error.Conflict("Appointment.PatientConflict", "This patient already has an appointment at this time.");

    public static readonly Error NotYetDue =
        Error.Conflict("Appointment.NotYetDue", "This appointment is not due yet. Check-in and treatment start on the day of the visit.");

    public static readonly Error SameStatus =
        Error.Validation("Appointment.SameStatus", "The appointment already has this status.");

    public static readonly Error PatientInactive =
        Error.Conflict("Patient.Inactive", "This patient is inactive. Reactivate them before booking.");

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

    public static readonly Error AppointmentTypeDuplicateName =
        Error.Conflict("AppointmentType.DuplicateName", "The clinic already has a service with this name.");

    public static readonly Error ProviderUnavailable =
        Error.Conflict("Appointment.ProviderUnavailable", "This provider has a scheduled leave or block during the selected time. Please choose a different date or another provider.");

    public static Error InvalidTransition(string from, string to) =>
        Error.Conflict("Appointment.InvalidTransition", $"Cannot transition from '{from}' to '{to}'.");

    public static readonly Error CannotReschedule =
        Error.Conflict("Appointment.CannotReschedule", "Only an appointment that has not started yet can be rescheduled.");

    public static readonly Error AlreadyTerminal =
        Error.Conflict("Appointment.AlreadyTerminal", "This appointment is in a terminal state and cannot be changed.");
}

