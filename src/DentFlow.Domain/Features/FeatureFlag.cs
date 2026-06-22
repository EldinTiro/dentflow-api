namespace DentFlow.Domain.Features;

public enum FeatureFlag
{
    // Identity
    MfaEnforcement,
    SsoOidc,

    // Staff
    MultiLocation,

    // Patients
    BulkPatientImport,
    PatientPortal,

    // Appointments
    RecurringAppointments,
    OnlineBooking,
    WaitlistManagement,

    // Billing
    InsuranceClaims,
    PaymentGatewayIntegration,
    AutomatedInvoicing,

    // Documents
    DocumentStorage,

    // Notifications
    SmsNotifications,
    CustomEmailTemplates,

    // Reporting
    AdvancedReporting,
    DataExport,
    CustomReports
}
