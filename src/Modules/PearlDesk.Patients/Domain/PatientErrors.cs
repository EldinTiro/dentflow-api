using ErrorOr;

namespace PearlDesk.Patients.Domain;

public static class PatientErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Patient.NotFound", "Patient was not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("Patient.AlreadyExists", "A patient with this email already exists.");

    public static readonly Error InvalidStatus =
        Error.Validation("Patient.InvalidStatus", "The provided patient status is not valid.");
}

