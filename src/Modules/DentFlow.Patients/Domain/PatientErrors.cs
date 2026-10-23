using ErrorOr;

namespace DentFlow.Patients.Domain;

public static class PatientErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Patient.NotFound", "Patient was not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("Patient.AlreadyExists", "A patient with this email already exists.");

    public static readonly Error PatientNumberAlreadyExists =
        Error.Conflict("Patient.PatientNumberAlreadyExists", "A patient with this patient number already exists.");

    public static readonly Error AlreadyInactive =
        Error.Conflict("Patient.AlreadyInactive", "This patient is already inactive.");

    public static readonly Error AlreadyActive =
        Error.Conflict("Patient.AlreadyActive", "This patient is already active.");

    public static readonly Error InvalidStatus =
        Error.Validation("Patient.InvalidStatus", "The provided patient status is not valid.");
}

