﻿namespace PearlDesk.Staff.Domain;

// Kept for any legacy string comparisons — prefer the StaffType enum for all new code.
// TODO: remove once all consumers are migrated.
[Obsolete("Use StaffType enum instead.")]
public static class StaffTypes
{
    public const string Dentist = "Dentist";
    public const string DentalAssistant = "DentalAssistant";
    public const string Hygienist = "Hygienist";
    public const string Receptionist = "Receptionist";
    public const string ClinicAdmin = "ClinicAdmin";
    public const string OfficeManager = "OfficeManager";

    public static readonly string[] All =
    [
        Dentist, DentalAssistant, Hygienist, Receptionist, ClinicAdmin, OfficeManager
    ];

    public static bool IsValid(string staffType) =>
        All.Contains(staffType, StringComparer.OrdinalIgnoreCase);
}

