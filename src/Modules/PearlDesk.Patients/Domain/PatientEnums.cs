namespace PearlDesk.Patients.Domain;

public enum PatientStatus
{
    Active,
    Inactive,
    Deceased,
    Transferred
}

public enum Gender
{
    Male,
    Female,
    NonBinary,
    Other,
    PreferNotToSay
}

public enum ContactMethod
{
    Mobile,
    Home,
    Work,
    Email,
    Sms
}

